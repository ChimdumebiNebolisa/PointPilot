# Post-implementation red-team review

## Claimed Outcome

PointPilot is a runnable Windows 11 voice-first companion with continuous Realtime speech, grounded Teach/Guide, interruptible GPT-5.6 Computer Use for foreground GIMP, local policy/confirmation, pointer overlay, verification, fixture, tests, packaging, and honest GIMP-only actuation scope.

## Strongest Failure Hypothesis

An interrupted or screen-injected plan could continue with a stale action or use an unconfirmed file target, leaving a partial GIMP edit or unintended export while PointPilot falsely reports success.

## Findings

### `MEDIUM Live GIMP workflow evidence is unavailable on this machine`

- Failure path: GIMP is absent, so target-HWND capture, GTK dialogs, action-search behavior, native undo, export UI, and three-run repeatability cannot be observed here.
- Evidence: executable discovery found no GIMP installation; live voice was not activated during automated UI inspection because it would transmit ambient audio.
- Impact: AC-10 and AC-20, plus the full live portions of AC-02 through AC-17, are not proven on the pinned environment.
- Adversarial test: execute `docs/live-test-checklist.md` from a fresh fixture three consecutive times on pinned GIMP 3.2.4, including spoken correction and Escape during action.
- Required mitigation: do not label this repository’s fake-backed tests as live evidence; block a public demo sign-off until the checklist passes.

### `MEDIUM UI capture and overlay are pinned to a narrow display environment`

- Failure path: non-100% scaling, remote sessions, minimized GPU windows, or a changed GIMP build may produce a black/incomplete capture or misaligned overlay.
- Evidence: physical-pixel target capture and WPF overlay are verified only by code/tests and the 100% desktop shell inspection, not a GIMP multi-DPI matrix.
- Impact: Teach pointing or model coordinates could be wrong outside the pinned setup; executor bounds still fail closed for out-of-image input.
- Adversarial test: repeat coordinate/capture checks at 125% and 150%, on a second monitor, and after move/resize/minimize.
- Required mitigation: keep the demo pinned to 100% single-monitor; add explicit DPI conversion and live matrix evidence before broadening support.

### `LOW Local filename enforcement depends on English export/save dialog titles`

- Failure path: a localized or differently titled file dialog might not trigger bare-filename enforcement.
- Evidence: path-like strings remain checked globally; confirmed full-path prompting and exact expected-file checkpoint verification remain active; the pinned UI is English.
- Impact: an unintended file side effect is possible before final verification if the model violates both prompt and dialog assumptions, although PointPilot will not claim the expected export succeeded.
- Adversarial test: inject a conflicting filename instruction into the GIMP canvas/dialog and verify the executor refuses it.
- Required mitigation: keep English UI pinned and add a dedicated dialog-state detector before localization support.

## Missing Evidence

- Microphone/WebRTC audio, five-turn conversational continuity, and live barge-in.
- Live target-HWND screenshots of GIMP with the overlay absent.
- One- and multi-action GPT-5.6 Computer Use in GIMP.
- Mid-action spoken correction, global Escape, native undo, confirmed overwrite/export, exact PNG, and three consecutive hero runs.

## Verdict

`PASS WITH RESIDUAL RISK`

The strongest stale-action, confirmation, prompt-injection, false-file-evidence, and foreground-drift paths have local mitigations and automated tests. API endpoints, compiled desktop surface, accessibility, packaging, dependency hygiene, and secret boundaries are verified. Residual risk is explicit and concentrated in the unavailable pinned GIMP/microphone live environment; it blocks public-demo acceptance evidence, not delivery of the implemented repository.
