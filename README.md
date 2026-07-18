# PointPilot

PointPilot is a compact, voice-first Windows companion. Start one session and
ask follow-up questions about the application in front of you. It can explain
what is visible, point to the target window, guide one step, and perform one
bounded, verified GIMP action when it can ground the request safely.

PointPilot is general-purpose for screen understanding and pointing. Foreground
GIMP 3.x on Windows 11 is the only verified mutation environment in this
release.

## Run from source

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -e ".[dev]"
Copy-Item .env.example .env.local
.\.venv\Scripts\pointpilot.exe
```

The standard OpenAI key remains in the ignored `.env.local` file and is used
only by the native PointPilot host. It is not rendered, logged, or sent to a
browser surface.

## Safety boundary

- One active task revision; speech and Escape invalidate stale work first.
- Capture only the external foreground target; PointPilot does not target itself.
- Mouse and keyboard mutation is restricted to foreground GIMP with unchanged
  bounds and target-relative coordinates.
- Screen/model text is untrusted data. Local validation controls every action.
- PointPilot verifies a target screenshot change before claiming an action
  completed. Export and overwrite requests stop for exact confirmation.

See `docs/clacky-provenance.md` for the pinned foundation and
`THIRD_PARTY_NOTICES.md` for required attribution.

