# PointPilot

PointPilot is a deterministic Windows desktop workflow automation and regression-testing tool. It records user interactions with native applications, converts them into a readable, versioned workflow specification, replays the workflow against the declared target application through stable Windows UI Automation selectors, verifies explicit postconditions, and produces a precise execution trace when anything fails.

It is a Playwright-like workflow system for native Windows desktop applications — with strong safety boundaries and deterministic verification.

## Core principles

1. **Deterministic execution.** The same workflow, compatible application state, and machine environment produce the same explicit control path.
2. **Stable target identification.** UI Automation properties (automation ID, accessible name, class, control type) are preferred; raw coordinates are an explicit last resort that is always flagged weak.
3. **Explicit verification.** Sending input is not success. Success requires declared postconditions: file existence, window state, control state/value, or exact image parity.
4. **Safe scope.** A run binds to one declared process and top-level window; every mutating step re-verifies foreground identity before any input is sent.
5. **Inspectable artifacts.** Workflows (YAML) and traces (JSON + human summary) are readable and versionable.
6. **No hidden inference.** Ambiguous or missing targets fail closed with actionable diagnostics — never a guess.
7. **Reproducible failure.** A failed run records which step failed, what was expected, what was observed, and what evidence was captured.

No AI, LLMs, model providers, API keys, cloud services, or paid infrastructure are used anywhere in the runtime.

## What is implemented

- Versioned YAML workflow schema (`schemaVersion: 1`) with strict parsing and line-numbered diagnostics for malformed or ambiguous definitions.
- Selector engine over Windows UI Automation: uniqueness checks, ambiguity detection, explicit `pick` for declared multiplicity, weakness flagging, fresh resolution before every action.
- State-machine replay engine: validate → bind → per-step resolve/act/verify, serialized input, cancellation at every atomic boundary.
- First-class assertions: file existence, window state, control existence/visibility/enabled/value, deterministic image parity against committed reference images.
- Recorder producing draft workflows from real interaction (UIA invoke/focus events plus foreground-filtered keyboard capture), with weak selectors flagged for review.
- Structured execution trace: JSON artifact plus concise human summary, including resolved-element records and diagnostic screenshots.
- WPF app lifecycle: choose target → record/load → inspect → dry-run/run → results and trace folder.
- CLI sharing the same parser and engine for local CI/scripted regression runs.

## Prerequisites

- Windows 11 x64 (Windows 10 x64 works but is not verified).
- .NET 8 SDK to build and test.
- No other services, accounts, keys, or network access are required at runtime.

## Build and verify

```powershell
dotnet restore PointPilot.sln --runtime win-x64 --locked-mode
dotnet build PointPilot.sln --configuration Release --no-restore
dotnet test PointPilot.sln --configuration Release --no-restore
dotnet format PointPilot.sln --verify-no-changes --no-restore --severity warn
```

Run the example end to end:

```powershell
notepad.exe   # start the target application first
dotnet run --project src/PointPilot.Cli/PointPilot.Cli.csproj --configuration Release --no-restore -- validate examples/notepad-demo.yaml
dotnet run --project src/PointPilot.Cli/PointPilot.Cli.csproj --configuration Release --no-restore -- run examples/notepad-demo.yaml --out traces/demo
```

Exit codes: 0 completed, 2 invalid workflow, 3 run failed, 4 cancelled, 1 usage error.

Desktop app:

```powershell
dotnet run --project src/PointPilot.App/PointPilot.App.csproj --configuration Release
```

## Package

```powershell
& .\scripts\package.ps1
```

Produces a self-contained Windows x64 zip under `artifacts/`.

## Documentation

Start with [workflow format](docs/workflow-format.md), [selector semantics](docs/selectors.md), [architecture](docs/architecture.md), [safety model](docs/safety-model.md), [testing](docs/testing.md), and the [migration note](docs/migration-model-removal.md) explaining removal of the earlier model-driven runtime. Historical Build Week artifacts remain under `specs/001-pointpilot/`.

## License

MIT. See `LICENSE` and `THIRD_PARTY_NOTICES.md`.
