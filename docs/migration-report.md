# Migration report

## Foundation

- Archived WPF implementation: annotated `pointpilot-wpf-archive` at
  `631b64ebd4fe557593263b8e92d24396368c6a6c`.
- Imported foundation: Clacky `e239089a4eb9daf7ac62d0f5c38e92fa53648499`.
- Attribution: retained in `THIRD_PARTY_NOTICES.md` for Raynan Wuyep and
  Shashank Singh.

## Retained and rebuilt

- Windows/PyQt companion, tray, non-intercepting pointer overlay, foreground
  target tracking, target-only capture, and bounded Windows input.
- Continuous native-host OpenAI Realtime session, VAD, speech interruption,
  session-scoped tool routing, revisioned action cancellation, and screenshot
  verification.

## Removed

- Clacky branding, Claude/Deepgram/ElevenLabs/provider switching, Google,
  Gmail, calendar, MCP, Composio, background agents, web research, routines,
  memory, skills, organizer, broad file tools, setup wizard, and legacy
  packaging.

## Remaining limitations

- The host terminated dependency installation before a live PyQt baseline or
  PointPilot manual GIMP run could complete. All live gates remain BLOCKED.
- Upstream Clacky's computer loop was a scaffold; PointPilot's action planner
  needs a live GIMP validation run before any demo or product reliability claim.
- Export/overwrite is deliberately fail-closed pending a dedicated exact-path
  confirmation UI.

