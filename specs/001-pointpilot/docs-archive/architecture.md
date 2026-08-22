# Architecture

## Component boundaries

```text
PointPilot.Core            (net8.0 — no Windows dependencies)
  Workflows/    WorkflowDefinition model, strict YAML parser + semantic validation,
                variable substitution, YAML emission
  Selectors/    SelectorSpec, criteria matching, resolution results (unique/zero/ambiguous),
                pick policy
  Elements/     IUiElement / IUiaSession abstractions
  Engine/       RunController (lease guard), RunStateMachine, ports, WorkflowRunner
  Tracing/      RunTrace model + human summary renderer
  Recording/    RecorderSessionBuilder: recorded events -> draft steps (pure logic)

PointPilot.Infrastructure  (net8.0-windows)
  Windows/      UiaSession + UiaElement (System.Windows.Automation adapter),
                WindowBinder (deterministic target binding), ForegroundMonitor,
                WindowsInputExecutor (the only SendInput caller), ScreenCaptureService,
                UiElementCatalog, NativeMethods
  Verification/ ExactImageComparer (pixel-exact assertions)
  Recording/    UiAutomationRecorder (UIA events + low-level keyboard hook)
  SystemClock / MachineInfoBuilder

PointPilot.App             WPF shell: lifecycle UI, tray, Escape hotkey, overlay feedback
PointPilot.Cli             console host sharing the identical parser and engine
```

The engine exists exactly once. The runner orchestrates over Core-defined interfaces; Infrastructure supplies the Windows implementations. Both hosts are thin.

## Execution flow

1. **Parse and validate.** YAML is parsed structurally; unknown keys, unknown step kinds, bad types, unsupported schema versions, undeclared variables, and invalid regexes are rejected with path-and-line diagnostics before anything runs.
2. **Substitute variables.** `${name}` placeholders are replaced from provided values or declared defaults; missing values abort before binding.
3. **Bind.** `WindowBinder` matches exactly one top-level window of the target process (exact or prefix name match; optional title regex). Zero or multiple candidates fail with diagnostics listing what was found.
4. **Run.** For each step the runner:
   - checks the run lease (cancellation) immediately before acting,
   - re-resolves selectors against a freshly enumerated UIA tree (no cached element references across steps),
   - requires the bound window to still be foreground, alive, and owned by the original pid for every input-emitting step (`focus-window` restores foreground explicitly when declared in the workflow),
   - sends input through the serialized executor,
   - evaluates wait/assert conditions against live state.
5. **Trace.** Every step records requested selector, resolved element identity and bounds, action attempted, observed postcondition, duration, status, failure reason, and evidence screenshot paths. Artifacts land in the chosen output directory as `trace.json` plus `summary.txt`.

## State machine

`Idle → Validating → Binding → Running → Completed | Failed | Cancelled`, with per-step sub-states recorded in the trace. Transitions are guarded by a locked transition table; illegal jumps are rejected and surfaced.

## Safety boundaries

- Only `WindowsInputExecutor` calls `SendInput`. Input is serialized by a semaphore; pressed modifiers are always released.
- Foreground verification happens immediately before every click/type/press; a pid change detects process restarts; bounds come from live window reads.
- Coordinate selectors map through `CoordinateMapper.RelativeToScreen` which rejects out-of-bounds coordinates.
- The recorder never sends input.
- No shell execution, no file deletion, no UAC interaction, no network calls exist in the runtime.

## Generalization

Supporting another application requires no code change: workflows declare their own target. GIMP-specific behavior does not exist anywhere in the codebase.
