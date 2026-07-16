# Pre-live acceptance baseline

Status: `PASS WITH RESIDUAL RISK`

This record preserves the automated baseline immediately before live microphone and GIMP acceptance. It does not claim that live acceptance has passed.

## Identity and artifact

- Final source identity: the commit referenced by the local annotated tag `pre-live-acceptance`.
- Resolve the exact final commit with `git rev-parse pre-live-acceptance^{commit}`.
- Verified implementation/package commit before adding this self-referential record: `47dc735f0cbb00ccf28e92bbf2ebd246f5b4a8ef`.
- Package: `artifacts/PointPilot-win-x64.zip` (ignored; not committed).
- Package SHA-256: `B49618CA1B2B9C96BCDF769EDDE9E1E80DD5DA25BAA690CF6EC043B66BE6D437`.
- Package size: 70,523,647 bytes.
- Package entries: 478.
- Recommended branch for evidence-driven live fixes: `test/live-gimp-acceptance`.

A Git commit cannot contain its own final object ID without changing that ID. The immutable tag reference above is therefore the canonical in-repository identity; the annotated tag message and handoff report record the literal tagged SHA.

## Automated evidence

| Check | Command | Result |
|---|---|---|
| npm restore | `npm ci` | PASS; 3 packages audited, 0 vulnerabilities |
| TypeScript | `npm run typecheck` | PASS |
| Web production build | `npm run build:web` | PASS |
| NuGet restore | `dotnet restore PointPilot.sln --runtime win-x64 --locked-mode` | PASS |
| Format | `dotnet format PointPilot.sln --verify-no-changes --no-restore --severity warn` | PASS |
| Release build | `dotnet build PointPilot.sln --configuration Release --no-restore` | PASS; 0 warnings, 0 errors |
| Tests and coverage | `dotnet test PointPilot.sln --configuration Release --no-restore --collect:"XPlat Code Coverage"` | PASS; 28 passed, 0 failed, 0 skipped; 510/805 lines and 239/575 branches |
| npm vulnerability audit | `npm audit --audit-level=high` | PASS; 0 vulnerabilities |
| NuGet vulnerability audit | `dotnet list PointPilot.sln package --vulnerable --include-transitive` | PASS; no vulnerable direct or transitive packages |
| Secret history scan | `gitleaks git --no-banner --redact .` | PASS; no leaks |
| Fixture reset | `& .\tools\generate-demo-fixture.ps1` twice | PASS; identical SHA-256 `352203CAE7133117905FC92168285DF2D3ED4E8C84EB93CC4858B0AEF8782C4E` |
| OpenAI endpoint smoke | `& .\tools\smoke-openai.ps1` | PASS; Realtime ephemeral credential and Responses endpoints |
| Package | `& .\scripts\package.ps1` twice from unchanged committed input | PASS; identical SHA-256 and clean worktree |
| Package boundary | ZIP entry and byte scan | PASS; required app/WebView/runtime files present; no env, skills, tests, fixtures, PDBs, logs, recordings, screenshots, coverage, credentials, or developer path |
| Development launch | Release executable, passive desktop inspection | PASS; reached Idle |
| Packaged launch | self-contained executable, passive desktop inspection | PASS; reached Idle |
| Accessibility | Windows accessibility tree and keyboard focus | PASS; state, context, conversation, Start, Mute, Pause, and End exposed; Tab focused Start |

The desktop checks did not select Start listening, grant microphone permission, transmit ambient audio, or operate GIMP.

## Known residual risks

- Five-turn live voice continuity and spoken barge-in remain unverified.
- Foreground GIMP target capture, overlay alignment, guarded visible input, correction, Escape, undo, export dialogs, screenshot verification, and exact PNG output remain unverified against GIMP 3.2.4.
- The full hero workflow must pass three consecutive counted runs.
- Capture and overlay evidence remains pinned to one 1920x1080 display at 100 percent scaling and English GIMP.

These items block public-demo acceptance, not the automated baseline. Do not broaden support claims or create speculative fixes before live evidence exists.

## Exact next live sequence

1. Create `test/live-gimp-acceptance` from `pre-live-acceptance`.
2. Install the official GIMP 3.2.4 Windows build and configure the environment exactly as described in `docs/live-test-checklist.md`.
3. Run the five-turn voice test without reactivation.
4. Run spoken barge-in while PointPilot is speaking.
5. Reset and run the one-action GIMP test.
6. Reset and run the Escape stop test.
7. Reset and run the mid-task correction test.
8. Reset and run the full hero workflow, exact export confirmation, PNG verification, and native undo checks.
9. Complete three consecutive counted hero runs, resetting after every attempt and preserving only the permitted evidence.

## Reset between counted runs

End PointPilot, close the fixture without saving test edits, run `& .\tools\generate-demo-fixture.ps1`, remove or manually archive the disposable export target, reopen `fixtures/pointpilot-promotional-graphic.ora`, restore the pinned GIMP docks/window/display state, confirm no modal dialog is open, and place GIMP in the foreground. Follow the full checklist rather than relying on this summary.
