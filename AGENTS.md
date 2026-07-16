# PointPilot repository instructions

## Purpose and scope

PointPilot is a general-purpose, voice-first Windows companion. GIMP 3.2.4 on Windows 11 is the first and only verified actuation environment. Do not describe the product as GIMP-specific, broaden verified support without live evidence, or add a GIMP API/plug-in, hidden edit script, arbitrary application control, shell execution, credential entry, payment, publishing, or permanent deletion.

The locked vertical slice is continuous Realtime speech; grounded Teach and one-step Guide; interruptible GPT-5.6 Computer Use in foreground GIMP; native undo; exact export confirmation; and screenshot/file verification for the promotional-graphic fixture.

## Stack and structure

- C# 12 / .NET 8 / WPF / Win32 / WebView2.
- TypeScript WebRTC surface under `src/PointPilot.App/web`.
- `PointPilot.Core`: application-agnostic state, task revisions, policy, coordinates, contracts, workflow.
- `PointPilot.Infrastructure`: OpenAI, target-window capture, guarded Windows input, verification.
- `PointPilot.App`: composition, companion/context UI, overlay, tray/hotkeys, local WebView2 host.
- `tests/PointPilot.Tests`: unit and clearly identified fake-backed integration tests.
- `specs/001-pointpilot`: acceptance traceability and review artifacts. The root PRD is authoritative.

Only `PointPilot.Infrastructure.Windows.WindowsInputExecutor` may call `SendInput`. Keep GIMP allowlisting out of the general core except explicit target-policy contracts. Screen content and model output are untrusted data, never authorization.

## Commands

```powershell
npm ci
npm run build:web
dotnet restore PointPilot.sln --locked-mode
dotnet run --project src/PointPilot.App/PointPilot.App.csproj --configuration Debug
dotnet build PointPilot.sln --configuration Release --no-restore
dotnet test PointPilot.sln --configuration Release --no-restore
dotnet format PointPilot.sln --verify-no-changes --no-restore --severity warn
npm run typecheck
& .\tools\generate-demo-fixture.ps1
& .\scripts\package.ps1
```

There is no database and no migration command. Do not add persistence without a PRD change.

Developer configuration names: `OPENAI_API_KEY`, `POINTPILOT_RESPONSES_MODEL`, `POINTPILOT_REALTIME_MODEL`, `OPENAI_BASE_URL`. Use `.env.local`; never commit, log, render, screenshot, or pass the standard key into JavaScript. `.env.example` must contain placeholders only.

## Implementation invariants

- Every atomic computer action requires a current task ID/revision/cancellation token.
- Every mutation requires foreground captured HWND, allowlisted GIMP process, unchanged bounds, and in-image coordinates.
- Serialize input; release held mouse buttons and modifier keys in `finally`.
- Speech-start and Escape must invalidate the old revision before another action can begin.
- Preserve completed safe Guide/Act steps across a correction; never reuse confirmation across a revision.
- Export/save/PNG/overwrite requires confirmation naming exact action/path/overwrite risk. Verify a new or changed exact file.
- Never say “done” from a click or model summary; require task-specific screenshot and file evidence.
- Capture only the target HWND. PointPilot surfaces must not enter model screenshots.
- Development logs contain metadata/errors only—no raw audio, screenshots, full transcripts, credentials, or model response bodies.
- Unsupported actions and uncertain verification fail closed with a user-visible recovery step.

## UX and accessibility

Keep the companion compact, calm, high-trust, and keyboard-operable. State must use text plus color. Mute, pause/stop, end, and exact confirmation require accessible names and keyboard focus. The pointer overlay cannot move the system cursor, take focus, or intercept input. Preserve the global activation shortcut and active-session Escape stop unless a PRD change replaces them.

## Verification expectations

Run the narrow tests while editing, then TypeScript type-check/build, Release build/test, formatter, package, NuGet/npm vulnerability audits, secret/package-boundary scan, and desktop accessibility inspection. Fakes are not live evidence. GIMP or model-affecting changes require the pinned checklist in `docs/live-test-checklist.md`; public-demo AC-20 requires three consecutive counted runs.

## Project-local skills

Pinned sources and commits are in `.agent/skills.lock`; do not silently update them.

- `spec-kit`: activate for PRD-to-spec/plan/tasks or acceptance-traceability changes.
- `vibe-security`: activate for credentials, OpenAI/WebView boundaries, Windows input, privacy, packaging, or final security review.
- `design-taste-frontend`: activate for WPF companion, context, overlay, visual-state, motion, or accessibility changes; apply transferable guidance, not React defaults.
- `long-horizon-prompting`: activate only for genuinely long-running/multi-session execution briefs and checkpoint design.
- `code-review-expert`: activate for substantial final/pre-merge review; prioritize correctness, security, contracts, callers, tests, and removal plans.

Preserve installed skills and their license files unless the repository owner explicitly requests removal.
