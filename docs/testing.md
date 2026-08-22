# Testing

## Automated suite

`dotnet test PointPilot.sln --configuration Release --no-restore` runs the xUnit suite (pure logic plus fake-backed engine integration; no real input is ever sent):

- **Parser** — valid workflows, unsupported schema versions, unknown step kinds, unknown fields, malformed YAML, coordinate/criteria conflicts, pick validation, press/wait/assert field rules, invalid regexes.
- **Substitution** — defaults, provided values, missing-variable diagnostics, substitution into text/paths/keys/selectors.
- **Selectors** — uniqueness, zero matches, ambiguity from duplicate accessible names, declared picks, out-of-range picks, weak-target classification.
- **Engine (fake ports)** — the adversarial matrix: ambiguous selectors fail closed, zero-match diagnostics include searched counts, elements disappearing between resolve and act send nothing, foreground loss aborts before input, process restart detection via pid change, coordinate out-of-bounds rejection, disabled elements refuse clicks, cancellation mid-run marks remaining steps non-failed, wait timeouts produce precise reasons, file assertions pass/fail in both directions, image threshold decides pass/fail, dry-run resolves selectors but sends no input, weak targets are flagged, trace artifacts are persisted.
- **Recorder builder** — invoke→click steps, typing accumulation and flushing, modifier presses, weak-selector flagging, guarantee that drafts never contain coordinate selectors.
- **Image comparer** — exact match, within-threshold deltas, beyond-threshold failures, size-mismatch rejection (real decoder).
- **Primitives** — bounds containment, relative-coordinate mapping with out-of-bounds rejection, element-center clamping, key normalization, secret redaction.

## Live verification path

Real UIA resolution, SendInput delivery, and PrintWindow capture require an interactive desktop session, which CI runners do not reliably provide. The reproducible local manual check is:

1. Build Release (`dotnet build -c Release`).
2. Start Notepad.
3. `dotnet run --project src/PointPilot.Cli -- run examples/notepad-demo.yaml --out traces/live`
4. Expect exit code 0, a `Completed` summary, and `traces/live/trace.json` containing resolved-element records for each step.

To exercise selectors against a richer tree, record a draft of any UIA-exposed application in the desktop app, save it, `validate` it, then `run` it with `--dry-run` first. Dry-runs bind and resolve every selector without sending input or writing files beyond trace artifacts.

## Quality gates

CI (`.github/workflows/ci.yml`) runs locked restore, `dotnet format --verify-no-changes`, Release build, tests, CLI smoke validation of the example workflow, self-contained win-x64 publish, and `dotnet list package --vulnerable --include-transitive`.
