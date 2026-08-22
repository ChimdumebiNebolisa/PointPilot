# PointPilot Baseline Adversarial Audit (pre-rebuild)

- **Audit status:** COMPLETE WITH LIMITATIONS (no live GIMP/model session was exercised; the rebuild task supersedes remediation of most findings)
- **Audit date:** 2026-08-21
- **Protocol:** `ADVERSARIAL_CODEBASE_AUDIT_PROTOCOL.md` (supplied out-of-band by the repository owner; it was **not** present in the cloned tree at the audited commit, contrary to the task statement — recorded as a baseline anomaly)
- **Mode:** Read-only. No source file was modified before this report.

## 17.2 Executive verdict

The repository at commit `631b64e` is exactly what its PRD says it is: an OpenAI Realtime voice + GPT-5.6 Computer Use companion for foreground GIMP on Windows 11. The implementation is small (113 tracked files), coherent, honestly documented about its live-evidence boundary, and its own checks pass locally. It is not a deterministic workflow tool and cannot be incrementally bent into one: the orchestration center (`PointPilotWorkflow`), all verification authority (`VerificationService` via visual model), and all target selection (screenshot coordinates chosen by a vision model) are model-driven. Overall codebase risk for the *new* product direction: **High** (wrong architecture, not broken code). Audit confidence: **High** for static claims; **Medium** for runtime behavior (no GIMP/live-model run was performed).

Top risks relative to the new deterministic direction:

1. All target identification is model-chosen screen coordinates; no UI Automation code exists anywhere in the repo despite the PRD listing "UI Automation before vision" as a reference idea.
2. `PointPilot.Core` contains product policy leaks: `TargetWindowPolicy.ValidateForMutation` hardcodes the `gimp` process allowlist inside the application-agnostic core.
3. Safety classification of goals is keyword matching over natural language (`ActionPolicy.ClassifyGoal`), trivially bypassable and meaningless without free-text goals.
4. Verification is "a vision model said so plus screenshot hash changed"; there is no deterministic postcondition system.
5. The entire web Realtime surface, OpenAI stack, voice state machine states, and hotkey session model have no role in the new product.

Strongest counterargument to a full rebuild: the Windows input/capture primitives are genuinely good, tested at the unit level, and could host a deterministic layer underneath the AI one ("keep both"). Rejected because the task explicitly forbids two competing products in one repository, and every user-visible surface (state names, confirmation flow, overlay semantics) is voice-session-shaped.

## 17.3 Repository state

| Item | Value |
|---|---|
| Remote | `https://github.com/ChimdumebiNebolisa/PointPilot.git` |
| Branch / commit | `main` / `631b64ebd4fe557593263b8e92d24396368c6a6c` (tag `pointpilot-wpf-archive`) |
| Working tree | Clean at audit start |
| Submodules / LFS | None used |
| Toolchain | .NET SDK 8.0.419, Node 24.14.1, npm 11.11.0, git-lfs 3.7.0 |
| OS | Windows 11 (10.0.26200), x64 |
| History | 24 commits, single linear line, no fix churn patterns |

## Baseline check reproduction (all run during this audit)

| Check | Command | Result | Output |
|---|---|---|---|
| Restore (locked) | `dotnet restore PointPilot.sln --runtime win-x64 --locked-mode` | Pass | 4/4 projects restored |
| npm install | `npm ci` | Pass | 2 packages, 0 vulnerabilities |
| Build | `dotnet build -c Release --no-restore` | Pass | 0 warnings, 0 errors (TreatWarningsAsErrors is on) |
| Tests | `dotnet test -c Release --no-restore` | Pass | 28/28 passed in ~1 s |
| TS type-check | `npm run typecheck` | Pass | clean |

Interpretation: green but shallow. The 28 tests exercise pure logic and fake-backed orchestration only. Nothing in CI or tests touches real UIA, real SendInput, or a real second process.

## Coverage ledger

All 113 tracked files were inventoried. Depth by group:

| Path group | Files | Depth | Status |
|---|---:|---|---|
| `src/PointPilot.Core/*.cs` (8 files) | 8 | Full read | Fully inspected |
| `src/PointPilot.Infrastructure/OpenAI/*` | 5 | Full read | Fully inspected |
| `src/PointPilot.Infrastructure/Windows/*`, `VerificationService.cs` | 4 | Full read | Fully inspected |
| `src/PointPilot.App/*.cs|xaml` | 9 | Full read | Fully inspected |
| `src/PointPilot.App/web/*` (ts, tsconfig, dist) | 5 | Full read (dist = generated) | Inspected; dist generated |
| `tests/PointPilot.Tests/*.cs` | 8 | Full read | Fully inspected |
| `docs/*.md` | 13 | Headers + targeted reads | Fully inspected (all stale after rebuild) |
| `specs/001-pointpilot/*.md` | 4 | Full read of spec/tasks, headers of rest | Fully inspected |
| Manifests/config (`*.csproj`, sln, props, package.json, lockfiles, `.env.example`, `.gitignore`, manifest) | 12 | Full read of authoritative ones; lockfiles metadata | Inspected |
| CI (`ci.yml`), `scripts/package.ps1`, `tools/*.ps1` | 3 | Full read | Fully inspected |
| `.agent/skills*` (33 files) | 33 | Provenance review via skills.lock | Vendored guidance, not runtime |
| `fixtures/*.ora`, LICENSE, THIRD_PARTY_NOTICES | 3 | Metadata/binary provenance | Partial (binary fixture; regenerable via script) |

Critical paths received full coverage. No file was skipped.

## Findings (evidence-grounded, relevant to the rebuild decision)

### F-001: Core hardcodes the GIMP allowlist
- Severity High (for new direction) · Confidence High · Verified
- `TargetWindowPolicy.cs:8` — `foregroundProcessName.StartsWith("gimp")` inside `PointPilot.Core`, which AGENTS.md itself declares must stay policy-free except explicit target-policy contracts.
- Consequence: any non-GIMP use requires editing core. New design must parameterize allowlists per workflow.

### F-002: Target selection is exclusively model-chosen image coordinates
- Verified. `ComputerUseService.RunAsync` feeds screenshots to Responses API; executor maps `(x,y)` through `CoordinateMapper`. No `System.Windows.Automation` reference exists anywhere (`grep` across tree: zero hits).
- Consequence: the required selector engine is net-new work, not salvage.

### F-003: Keyword-based goal safety classification
- `ActionPolicy.ClassifyGoal` (`ComputerActions.cs:29`) matches substrings like `"png"`, `"save"`, `"shell"` against free text. A goal phrased "write the result to out.png" classifies Consequential only by luck; "empty the recycle bin" is not Prohibited. Model-era heuristic; superseded by explicit step-level policy in workflows.

### F-004: Verification authority is a vision model
- `VerificationService.VerifyAsync` requires SHA256 difference between before/after PNGs (fails for legitimately unchanged-but-correct end states) and delegates certainty to `IVisualReasoningService`. File checks exist and are sound (checkpoint comparison prevents stale-file success). Salvageable idea: file checkpoints.

### F-005: Task lease/revision machinery is genuinely generic
- `TaskCoordinator` (159 lines): revision invalidation, confirmation bound to exact revision+action+path, serialized executor gate (`WindowsInputExecutor.EnsureCurrent`). Directly reusable as the per-run/per-step execution guard. Tests cover interruption, confirmation rebinding, pause cancellation.

### F-006: State machine shape is voice-session-specific
- States `Connecting/Listening/Speaking/Teaching/Guiding` etc. (`PointPilotStateMachine.cs`) have no meaning for replay runs. Pattern (locked transition table + rejected-transition event) is reusable; instance is not.

### F-007: Windows primitives are small, correct-looking, and reusable
- `NativeMethods` (25 lines), `WindowContextService` (PrintWindow flag 2 = PW_RENDERFULLCONTENT, bounds from GetWindowRect, process id → name), `WindowsInputExecutor` (serialized SemaphoreSlim, modifier press/release in finally, drag mouse-up in finally, key normalization, VK mapping). Caveats: `TypeText` sends Unicode events per char (fine for SendInput); `MouseUp` moves cursor when X/Y supplied (asymmetric with MouseDown); `Wait` clamps to 10 s. DPI: manifest sets PerMonitorV2; physical-pixel coordinates consistent throughout.

### F-008: App composition root is WebView2/tray/voice-shaped
- `MainWindow.xaml.cs` (491 lines) wires realtime messages to workflow tools. Overlay window uses WS_EX_TRANSPARENT|WS_EX_NOACTIVATE correctly (never takes focus/input). Tray, global hotkey service, foreground-window restore tracker, NDJSON dev log with redaction: all reusable shell concepts; wiring is not.

### F-009: Docs match implementation but describe the old product
- README claims verified against code: all accurate (voice, GIMP pinning, evidence boundary). Contradiction exists only against the NEW direction. `docs/architecture.md` explicitly describes the model data flow. All active docs require rewrite; historical docs move out of the active path.

### F-010: CI/packaging path is sound and reusable
- `ci.yml`: windows-latest, locked restore, format verify, build, test, publish self-contained win-x64, vulnerable-package audit, npm audit. `package.ps1`: deterministic zip (fixed timestamp), pdb strip, license copy. Both survive the rebuild nearly unchanged minus web steps.

### F-011: Secrets hygiene
- `.env.example` placeholders only; key loaded from env/.env.local; redaction regex applied to logs/tool outputs; WebView2 gets ephemeral secret only. No secret material found in tree. After removing OpenAI entirely, the whole env-contract surface disappears (desired).

### F-012: Recorder does not exist
- There is no hook/event-listening/UIA-subscription code that could seed a recorder. Net-new capability.

## Contradictions table (old docs vs new mandate)

| Source A | Source B | Contradiction | Resolution |
|---|---|---|---|
| PRD §29 "Voice is mandatory", "GPT-5.6 Computer Use is the primary actuation planner" | New task: no AI/LLM/model providers | Product identity conflict | Task-specific instructions control; PRD archived as historical |
| AGENTS.md "Keep GIMP allowlisting out of general core" | `TargetWindowPolicy.cs:8` | Code violates stated invariant | Rebuild parameterizes policy |
| README "What is implemented" list | New completion criteria | Every bullet obsolete | Rewrite |

## What can be deleted safely (verified by caller analysis)

- `Infrastructure/OpenAI/*` (5 files): referenced only by `MainWindow.xaml.cs`, `VerificationService`, tests.
- `web/**` + WebView2 package + npm scripts: referenced only by MainWindow/App csproj/package.json/CI.
- `PointPilotWorkflow`, voice state enum, `ErrorMapper.IntegrationFailure` OpenAI members, `SecretRedactor.OpenAiKey` (keep generic redaction if logging survives).
- Voice-shaped UI surfaces and hotkey session semantics.

## What is preserved

- `Primitives.cs` wholesale (ScreenPoint/WindowBounds/CoordinateMapper/KeyNormalizer) — already covered by tests.
- `TaskCoordinator` concept (leases/revisions/cancellation/confirmation) simplified for run/step guarding.
- State-machine pattern reimplemented with replay states.
- `NativeMethods`, capture service (generalized beyond foreground-only), guarded input executor (allowlist parameterized).
- Overlay window technique (non-focus, non-input, excluded-from-capture) for pointing at resolved targets during debugging.
- Deterministic packaging script, CI skeleton, locked restore discipline, TreatWarningsAsErrors culture.
- Test project scaffolding (xunit), fixture-generation approach for demo assets.

## Residual unknowns

- Live behavior of PrintWindow on GPU-composited windows in this environment (not exercised during audit; flagged for manual verification checklist).
- Whether target apps expose usable UIA trees (per-app property; the selector engine must fail closed with diagnostics).
- Real GIMP fixture behavior remains untested here; GIMP may remain a demo fixture only.
