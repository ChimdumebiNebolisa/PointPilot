# Pinned demo environment

The release validation target is:

- Windows 11, x64, current supported security updates, display scaling 100%, single 1920×1080 primary monitor.
- GIMP 3.2.4, official universal Windows x64 installer, default English UI, Single-Window Mode.
- PointPilot `Release` self-contained `win-x64` package.
- Microsoft Edge WebView2 Runtime 150.0.4078.65 or newer compatible evergreen runtime.
- Default PointPilot models: `gpt-realtime-2.1`, `gpt-4o-mini-transcribe`, and `gpt-5.6` Responses/Computer Use.
- A working default microphone and speakers/headphones.

GIMP 3.2.4 is the pinned application version because it is the current stable bug-fix release selected during bootstrap. Do not silently substitute a development build; record any newer patch version as a new evidence environment.

## Fixture setup

1. Run `tools/generate-demo-fixture.ps1` from the repository root.
2. Open `fixtures/pointpilot-promotional-graphic.ora` in GIMP.
3. Confirm the Layers panel shows, from top to bottom: Accents, Product badge, Subtitle — edit this in the demo, Title, Focus visual, Background.
4. Set GIMP to Single-Window Mode and maximize it.
5. Make GIMP foreground before any Teach, Guide, Act, Undo, or resume turn.
6. Use a clean export target such as `%USERPROFILE%\Pictures\pointpilot-built-for-focus.png` and record whether it already exists.

The fixture generator creates a 1440×900 OpenRaster composition from original programmatic shapes and text. It does not use external assets or hidden GIMP automation.
