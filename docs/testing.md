# Testing

## Automated

```powershell
npm ci
npm run typecheck
npm run build:web
dotnet restore PointPilot.sln --runtime win-x64 --locked-mode
dotnet format PointPilot.sln --verify-no-changes --no-restore --severity warn
dotnet build PointPilot.sln --configuration Release --no-restore
dotnet test PointPilot.sln --configuration Release --no-restore --collect:"XPlat Code Coverage"
dotnet publish src/PointPilot.App/PointPilot.App.csproj --configuration Release --runtime win-x64 --self-contained true
dotnet list PointPilot.sln package --vulnerable --include-transitive
npm audit --audit-level=high
```

The automated suite uses explicit fakes for screenshots, visual results, Computer Use, and verification. It verifies orchestration and safety behavior; it is not evidence of live GIMP or live model success.

## Live

Use the pinned environment and `docs/live-test-checklist.md`. A counted hero run begins when PointPilot enters Acting with foreground GIMP. Setup failures—missing model entitlement, microphone denial, GIMP absent, or bad network before action—are recorded but do not count as workflow failures. Once acting begins, any incorrect action, stale action after correction/Escape, false completion, missed confirmation, missing output file, or operator rescue fails the run.

AC-20 passes only after three consecutive counted successful runs from a fresh fixture state. Preserve a screenshot of the initial state, the final GIMP state, the exported PNG metadata/path, timestamps, and the checklist for each run. Never preserve API keys or raw audio.
