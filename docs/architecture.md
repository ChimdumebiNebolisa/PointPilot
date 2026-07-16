# Architecture

## Runtime boundary

`PointPilot.Core` owns general state, immutable task leases, revision invalidation, policy, coordinate mapping, and application-agnostic workflow orchestration. `PointPilot.Infrastructure` supplies Windows target capture/input, OpenAI Realtime-token issuance, GPT-5.6 visual reasoning and Computer Use, and verification. `PointPilot.App` is the WPF shell, overlay, tray/hotkeys, and a one-pixel local WebView2 WebRTC surface.

The browser surface is served from an HTTPS virtual hostname mapped to packaged local files. Host objects, DevTools, default dialogs, cross-origin host resource access, and non-microphone permissions are disabled. The .NET host mints a short-lived client secret and passes only its `value` to JavaScript. The standard key never crosses this boundary.

The companion tracks the latest external foreground HWND. If a user clicks the companion’s Start or Confirm control, PointPilot restores only that previously foreground window before capture; it never steals focus from a different external application selected afterward. Act mode still independently requires foreground GIMP at every mutation.

## Action data flow

1. Realtime interprets direct user speech and calls `teach`, `guide`, `act`, or `undo`.
2. Teach/Guide capture only the foreground target HWND, ask GPT-5.6 for grounded JSON, and render an independent non-activating pointer overlay.
3. Act classifies the goal. Prohibited goals stop; consequential goals pause in Planning for exact confirmation.
4. The coordinator issues a task lease containing task ID, revision, and cancellation token.
5. GPT-5.6 Computer Use receives the screenshot and goal. For each batch, the executor checks the lease immediately before every atomic action.
6. Mutating input additionally requires the same foreground HWND, a process name beginning with `gimp`, unchanged window bounds, and coordinates inside the captured image.
7. After each Computer Use batch, PointPilot recaptures GIMP. At completion, the verification service requires a visible screenshot change, conservative visual confirmation, and—when applicable—the exact expected file to exist.
8. Only verified outcomes produce “done” language.

## Interruption and race control

Speech-start during Planning, Acting, or Verifying cancels the old lease and increments the revision before the correction is interpreted. The serialized executor checks `IsCurrent` immediately before each action, so queued old-plan actions fail closed. Completed safe steps stay on the task. The correction revises the goal and constraints and triggers a fresh screenshot/Computer Use plan.

Escape is a global hotkey only while a session is active. It cancels the active tool/task, cancels model speech, releases an in-progress drag in a `finally` block, hides the overlay, and enters Paused.

## Capture and overlay

Capture uses `PrintWindow` against the foreground target handle rather than copying the desktop. PointPilot windows are different HWNDs and therefore absent from the image sent to the model. Computer coordinates are screenshot pixels and are mapped to the current captured window bounds only after bounds validation.

## Generalization seam

Supporting another verified application requires a deliberate target-policy adapter and live acceptance evidence. It does not require replacing Realtime, the task coordinator, state machine, visual contracts, confirmation semantics, or verification contracts.
