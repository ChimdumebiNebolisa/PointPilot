# Troubleshooting

- **Missing or rejected key:** create `.env.local` from `.env.example`, set a project-scoped key, and restart. Never paste it into the UI or JavaScript console.
- **Microphone denied:** enable microphone access for desktop apps and Microsoft Edge WebView2, then end and restart the session.
- **Ctrl+Alt+Space unavailable:** another program owns it; use the tray icon or Start listening button for this build.
- **Act refuses:** maximize and focus the pinned GIMP window. PointPilot deliberately refuses if the handle, process, or bounds differ from the captured target.
- **Capture is black or incomplete:** disable unusual compositor/remote-session modes, restore GIMP from minimized state, use the pinned local Windows environment, and retry from a fresh screenshot.
- **PointPilot pauses after a window change:** return GIMP to foreground, inspect partial edits, use native undo if appropriate, then resume with an explicit instruction.
- **Verification uncertain:** inspect the document and expected path. PointPilot will not convert uncertainty into success.
- **Export did not occur:** no confirmation is transferable across an interruption. Repeat the exact export request/path and confirm the new revision.
- **WebView2 missing:** install the Microsoft Edge WebView2 Evergreen Runtime, then relaunch the packaged app.
