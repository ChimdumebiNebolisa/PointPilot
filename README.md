# PointPilot

PointPilot is a general-purpose, voice-first Windows companion that can explain the live interface in front of you, point to relevant controls, guide one verified step at a time, and—when asked—operate a supported application through visible mouse and keyboard actions.

GIMP is the first verified actuation environment, not PointPilot’s product identity. The core coordinator, state machine, capture, policy, verification, Realtime voice host, and computer-action contracts are application-agnostic. This release keeps mutation allowlisted to foreground GIMP 3.x.

## What is implemented

- Continuous OpenAI Realtime speech over WebRTC after one activation, with server VAD and barge-in.
- Teach, Guide, Act, and Undo tools sharing one session.
- Target-window capture and GPT-5.6 visual grounding.
- Pointer overlay that does not move the real cursor and is excluded by target-HWND capture.
- GPT-5.6 Computer Use action loop with visible Windows input.
- Serialized atomic actions guarded by task ID, revision, cancellation, foreground handle/process, unchanged bounds, and coordinate checks.
- Exact export confirmation bound to task revision, action, and path.
- Screenshot-based verification plus expected-file existence checks before success language.
- Global `Ctrl+Alt+Space`, tray controls, keyboard-accessible controls, and global Escape stop while a session is active.
- Reproducible layered OpenRaster promotional-graphic fixture.

## Prerequisites

- Windows 11 x64.
- .NET 8 SDK (the installed bootstrap environment did not contain .NET 10; the code is intentionally compatible with the available supported LTS SDK).
- Node.js 24+ for building the small local TypeScript Realtime surface.
- Microsoft Edge WebView2 Runtime.
- GIMP 3.2.4 for the pinned live-demo environment.
- An OpenAI API project key with access to the configured Realtime, Responses, vision, transcription, and Computer Use models.

## Build and run

```powershell
Copy-Item .env.example .env.local
# Edit .env.local and set OPENAI_API_KEY. Do not quote or commit it.
npm ci
npm run build:web
dotnet restore PointPilot.sln --runtime win-x64 --locked-mode
dotnet test PointPilot.sln --configuration Release
dotnet run --project src/PointPilot.App/PointPilot.App.csproj --configuration Release
```

Generate the fixture before a demo:

```powershell
& .\tools\generate-demo-fixture.ps1
```

Open `fixtures/pointpilot-promotional-graphic.ora` in GIMP, keep GIMP foreground, and start PointPilot with `Ctrl+Alt+Space`. The long-lived API key stays in the .NET host. WebView2 receives only a short-lived Realtime client secret in memory.

## Package

```powershell
& .\scripts\package.ps1
```

This produces a self-contained Windows x64 zip under `artifacts/`. WebView2 Runtime and GIMP remain declared machine prerequisites.

## Scope and evidence

Start with [product scope](docs/product-scope.md), [architecture](docs/architecture.md), [pinned demo environment](docs/demo-environment.md), [demo script](docs/demo-script.md), [security and privacy](docs/security-and-privacy.md), and [testing](docs/testing.md). The authoritative requirements remain in `PointPilot_PRD.md`; acceptance traceability is in `specs/001-pointpilot/`.

All repository implementation files are new Build Week work. No pre-existing product source was present in the cloned repository. Project-local third-party skill instructions are provenance-pinned in `.agent/skills.lock`; they are development guidance, not runtime code.

## Honest verification boundary

Automated tests and local build/package checks are reproducible without GIMP. Live AC-10 and AC-20 require the pinned GIMP build, microphone, model access, and three consecutive operator runs. A run is not counted when a prerequisite fails before PointPilot begins acting. See `docs/live-test-checklist.md` for the exact evidence form.

## License

MIT. See `LICENSE` and `THIRD_PARTY_NOTICES.md`.
