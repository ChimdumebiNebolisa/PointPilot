# PointPilot implementation plan

## Technical context

- C# 12, .NET 8, WPF, Win32 interop, WebView2
- TypeScript Realtime WebRTC client compiled to static WebView assets
- Raw `HttpClient` integrations for explicit OpenAI wire contracts
- xUnit unit and fake-backed integration tests
- Windows 11 x64, single monitor, 1920x1080, 100 percent scaling

## Architecture decisions

1. `PointPilot.Core` contains application-agnostic state, policies, task context, contracts, and verification semantics.
2. `PointPilot.Infrastructure.Windows` owns capture and input. It is the only assembly allowed to call `SendInput`.
3. `PointPilot.Infrastructure.OpenAI` owns standard-key API calls. The browser receives only a client secret.
4. `PointPilot.App` owns WPF surfaces, tray/hotkeys, WebView lifecycle, and composition.
5. All actuation is serialized through one coordinator task lease; every action is checked at the atomic execution boundary.
6. GIMP knowledge lives in demo goals and verification predicates, not core architecture or a plugin.

## Security boundaries

- `.env.local` is ignored; `OPENAI_API_KEY` never crosses into JavaScript or logs.
- WebView messages use a narrow discriminated schema and no exposed host object.
- Screenshot content and model output are untrusted.
- Only foreground `gimp`/`gimp-3*` processes are allowed for mutation.
- Save/export/overwrite require confirmation bound to the current revision.
- Password/UAC/payment/shell/publish/delete actions are prohibited.

## Rollback and recovery

- Escape or speech-start cancels the task lease and pauses at the next atomic boundary.
- GIMP reversible mutations may use native undo; external exports are not represented as undoable.
- Window drift or verification uncertainty pauses without further action.
- The repository is greenfield, so rollback is file-level removal against the initial empty inventory.

## Verification gates

1. Core gate: state/policy/revision/redaction tests pass.
2. Infrastructure gate: fake and Win32 boundary tests pass without sending real input.
3. API gate: request/response fixture tests and TypeScript checks pass.
4. UI gate: WPF build and startup smoke pass.
5. Release gate: all tests, secret scan, dependency audit, packaging, code review, and red-team pass.
6. Live gate: Realtime plus three GIMP hero runs on the pinned environment.
