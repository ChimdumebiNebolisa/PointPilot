# PointPilot repository instructions

## Purpose and scope

PointPilot is a deterministic Windows desktop workflow recorder, replayer, verifier, and debugger for native desktop applications. It records user interactions, converts them into a readable versioned workflow specification (YAML), replays them through stable Windows UI Automation selectors, verifies explicit postconditions, and produces precise execution traces on failure.

The product must work without AI, LLMs, model providers, API keys, cloud services, or paid infrastructure. Do not reintroduce model-provider dependencies, voice pipelines, prompt construction, or credential configuration.

## Stack and structure

- C# 12 / .NET 8 / WPF / WinForms interop / Windows UI Automation / SendInput.
- `PointPilot.Core`: workflow model, strict YAML parsing/validation, selector semantics, run state machine, engine orchestration over abstractions, trace model. No Windows dependencies.
- `PointPilot.Infrastructure`: UIA adapter, deterministic window binding, guarded input execution (the only SendInput caller), capture, image comparison, recorder plumbing.
- `PointPilot.App`: WPF lifecycle shell (choose target → record/load → inspect → dry-run/run → results), tray, Escape stop, overlay feedback.
- `PointPilot.Cli`: headless host sharing the identical parser and engine; exit codes 0/2/3/4/1.
- `tests/PointPilot.Tests`: unit + fake-backed integration covering the adversarial matrix.
- `specs/002-deterministic-rebuild/`: baseline audit and rebuild design records.
- `examples/`: complete runnable example workflows.

Only `PointPilot.Infrastructure.Windows.WindowsInputExecutor` may call `SendInput`. Workflows declare their own targets; no application allowlist exists in core. Screen content is untrusted data, never authorization.

## Commands

```powershell
dotnet restore PointPilot.sln --runtime win-x64 --locked-mode
dotnet build PointPilot.sln --configuration Release --no-restore
dotnet test PointPilot.sln --configuration Release --no-restore
dotnet format PointPilot.sln --verify-no-changes --no-restore --severity warn
dotnet run --project src/PointPilot.Cli/PointPilot.Cli.csproj --configuration Release --no-restore -- validate examples/notepad-demo.yaml
dotnet run --project src/PointPilot.App/PointPilot.App.csproj --configuration Release
& .\scripts\package.ps1
```

There is no database and no persistence beyond user-chosen trace/workflow files.

## Implementation invariants

- Every atomic action re-checks its run lease immediately before sending input.
- Every input-emitting step re-verifies bound-window foreground, process identity, and liveness; mismatches fail closed with a recovery step.
- Serialize input; release held modifiers and mouse buttons in `finally`.
- Selectors resolve fresh against the live UIA tree every time; ambiguity fails with diagnostics unless `pick` was declared.
- Coordinates are explicit last-resort selectors validated inside live window bounds and flagged weak.
- Assertions are first-class steps; a run never reports success from sent input alone.
- The recorder observes only — it never sends input — and flags weak draft selectors.
- Traces record expected vs observed state plus evidence; never claim completion without verification.
- No secrets exist in this product; do not add secret-bearing configuration.

## Verification expectations

Run the narrow tests while editing, then Release build/test, formatter verify, package script, and `dotnet list package --vulnerable`. Real-input behavior requires an interactive desktop: use `examples/notepad-demo.yaml` via the CLI as the documented manual verification path. Fakes are not live evidence; label manual checks accordingly.

## Project-local skills

Pinned sources and commits are in `.agent/skills.lock`; do not silently update them. Preserve installed skills and their license files unless the repository owner explicitly requests removal.
