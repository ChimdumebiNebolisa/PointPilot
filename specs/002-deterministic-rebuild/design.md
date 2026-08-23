# PointPilot Deterministic Rebuild - Implementation Design

Grounded in `audit-baseline.md` (commit `631b64e`). Task-specific rebuild requirements control product direction; conflicts with the audit protocol are resolved in favor of the task (protocol section 1).

## Product

PointPilot is a deterministic Windows desktop workflow automation and regression-testing tool: record user interactions against native applications, store them as readable versioned workflow specifications, replay them against native Windows applications through stable UI Automation selectors, verify explicit postconditions, and produce precise execution traces on failure. No AI, LLMs, model providers, API keys, cloud services, or paid infrastructure anywhere in the runtime.

## Component boundaries

```text
PointPilot.Core            (net8.0, no Windows deps, fully unit-testable)
  WorkflowDefinition model, YAML parsing + validation diagnostics,
  variable substitution, Selector model + matching semantics,
  RunStateMachine, Trace model, assertion evaluation contracts,
  execution engine orchestration over abstractions
PointPilot.Infrastructure  (net8.0-windows)
  UIAElementTree : real System.Windows.Automation adapter,
  WindowsInputExecutor : serialized SendInput (preserved, parameterized policy),
  WindowLocator / WindowBinder : process+window enumeration and run binding,
  ScreenCaptureService : PrintWindow by HWND (generalized from foreground-only),
  AssertionEvaluator : file/window/control/image checks,
  TraceWriter : JSON + summary artifacts,
  UiAutomationRecorder : focus/invoke/keyboard event capture -> draft workflow
PointPilot.App             (WPF shell)
  Lifecycle: choose target -> record/load -> inspect -> run/dry-run -> results.
  Tray icon, Escape emergency stop during runs, overlay flash of resolved targets.
PointPilot.Cli             (new, console)
  Same parser + engine; headless local CI/scripted runs. Exit codes 0/2/3.
```

The engine exists exactly once (Core orchestrates over Core-defined abstractions; Infrastructure supplies Windows implementations). CLI and App are thin hosts.

## New dependency

YamlDotNet (16.x) - single well-maintained package enabling the required human-readable versioned format. Everything else stays in-box (`System.Windows.Automation` ships with the Windows desktop runtime pack already referenced via UseWPF/UseWindowsForms).

## Workflow schema v1 (YAML)

```yaml
schemaVersion: 1
name: notepad-demo
description: optional human description
variables:              # substituted into string fields via ${name}
  note: hello
defaults:
  timeoutMs: 5000       # inherited per-step wait/assert timeout
target:
  processName: Notepad  # exact match (case-insensitive) unless processNameMatch: prefix
  processNameMatch: exact
  windowTitle: .*Notepad.*    # optional regex against main window title
steps:
  - step: focus-window
  - step: click
    selector: { automationId: closeButton }
  - step: double-click
    selector: { className: DocCanvas, pick: index:0 }   # pick makes multi-match explicit
  - step: type-text
    text: "${note} world"
    selector: { role: edit }         # optional; types into focused control when omitted
  - step: press
    keys: [CTRL, S]
  - step: wait
    until: { windowTitle: ".*Untitled.*" }
    timeoutMs: 3000
  - step: screenshot                 # diagnostic evidence into the trace directory
  - step: assert-file
    path: C:/out/demo.txt
    condition: exists                # exists | not-exists
  - step: assert-window
    condition: visible               # visible | minimized | closed | foreground
  - step: assert-control
    selector: { name: Save, controlType: button }
    state: enabled                   # exists | visible | enabled | value
    value: "42"                      # required for value (UIA ValuePattern)
  - step: assert-image               # optional, fully deterministic
    selector: { automationId: canvas }
    referenceImage: refs/canvas.png  # committed local PNG
    maxChannelDelta: 8               # strict per-channel tolerance
  - step: click
    selector: { x: 120, y: 40 }      # explicit last-resort coordinates (validated in-bounds, flagged weak)
```

Validation fails closed with actionable diagnostics (path + message): unknown schemaVersion, unknown step kinds, unknown fields rejected, missing/duplicate names, empty selectors, `${var}` referencing undeclared variables, coordinate selectors missing x/y, non-positive timeouts, regex compile errors, reference images not found.

## Selector semantics

Priority order (all explicit in the file, never silent fallback): `automationId` > `name`(+optional `controlType`) > `className`(+role) > ancestor-scoped variants of those > `coordinates` (last resort, always flagged weak in traces). Resolution walks the bound window's UIA tree, collects ALL matches; zero matches = failure with searched-criteria diagnostics; multiple matches = failure unless `pick:` is declared (then recorded as weak). Every resolution records the found element's AutomationId/name/class/controlType/bounding rect in the trace. Re-resolution happens immediately before every action; stale cached elements are never reused.

No image-based element targeting in v1. Deterministic image checking exists only as `assert-image` (exact-region per-channel delta against a committed reference PNG).

## Run state machine

`Idle -> Validating -> Binding -> Running -> Completed | Failed | Cancelled`
Running iterates steps: `Resolving -> Acting -> Verifying` per step (internal sub-states tracked in trace, guarded transitions reject illegal jumps like Binding -> Completed).

## Safety model (preserved/strengthened from audit F-005/F-007)

- One active run per process. Every mutating step executes under a lease checked immediately before send (task id/revision/cancellation preserved conceptually from TaskCoordinator, simplified to RunLease).
- Run binding: process id + top-level window handle captured at start; every foreground-required step re-verifies: bound HWND still foreground, PID alive, bounds unchanged. Mismatch => fail closed with recovery message, never act into the wrong application.
- Input serialization via semaphore; held modifiers/mouse released in finally (preserved verbatim from audited executor).
- Escape (App) / Ctrl+C (CLI) cancel at the next atomic boundary; speech-start invalidation concept becomes generic revision bump.
- Coordinates only from explicit workflow values, validated inside current window bounds.
- No shell execution, no file deletion, no UAC interaction, no privilege elevation anywhere in the engine. Screenshots/assertion outputs write only under the caller-provided trace/output directories.
- Recorder never sends input; observation only.

## Execution trace

JSON artifact + human summary per run: runId, workflow name/schemaVersion/content hash, target identity (pid/process/title/handle), machine metadata (OS version, CLR version, bitness, screen size), start/end UTC times, per-step records (kind, name, requested selector, resolved element info, action attempted, observed postcondition, durationMs, status, failure reason, evidence screenshot path), totals. Failed required assertion stops the run; trace marks the failing step and everything observed up to it.

## Deletion plan (legacy model runtime)

Delete: `Infrastructure/OpenAI/*` (5 files), `web/**` + WebView2 package + npm manifests/scripts, `PointPilotWorkflow`, voice state enum instance, OpenAI-shaped ErrorMapper/SecretRedactor/OpenAiOptions usage, `tools/smoke-openai.ps1`, `.env.example`, GIMP fixture tool + binary fixture (superseded by `examples/notepad-demo.yaml` runnable headlessly), PRD-era docs rewritten or moved to `specs/001-pointpilot/` historical path. AGENTS.md rewritten for the new product. CI loses Node steps, gains CLI smoke.

## Migration / salvage ledger

| Old | Fate |
|---|---|
| Primitives.cs (points/bounds/mapper/key normalizer) | Preserved as-is |
| TaskCoordinator leases/revisions/confirmation | Simplified into RunLease + revision guard |
| State machine transition-table pattern | Reimplemented with run states |
| NativeMethods + WindowsInputExecutor | Preserved, allowlist parameterized, MouseUp asymmetry fixed |
| WindowContextService PrintWindow capture | Generalized to HWND-parameterized capture |
| OverlayWindow technique | Preserved for resolved-target feedback |
| VerificationService file checkpoints | Idea preserved in assert-file evaluator |
| DevelopmentLog redaction habit | Kept (trace contains no secrets by construction; redactor kept for defense in depth) |
| OpenAI stack, web surface, voice flow, keyword goal policy, vision verification | Deleted |

## Verification strategy

Unit tests over Core (parser, validator, substitution, state machine, selector matching on a fake element tree, engine orchestration with fake executor/capture/clock). Adversarial suite covers the 18 mandated scenarios. Real-UIA integration smoke gated behind `POINTPILOT_UIA_LIVE=1` (GitHub runners lack reliable interactive desktops); a documented manual verification path covers live replay. Build/test/format/package/audit all green or explicitly blocked-and-documented.
