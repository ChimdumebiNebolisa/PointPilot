# Safety model

PointPilot automates real desktop applications; the safety model is designed so that a workflow can never silently act into the wrong window, wrong process, or wrong place.

## Binding

A run binds once to exactly one top-level window: declared process name (exact or prefix) plus optional title regex. Zero or multiple candidates abort with diagnostics. The binding records pid, process name, handle, and title in the trace.

## Per-step target re-verification

Every input-emitting step (clicks, typing, key presses) verifies immediately before sending that:

1. the bound window handle is still alive,
2. it is still the foreground window,
3. its owning process id is unchanged (detects restarts and window-handle reuse).

Any mismatch fails closed with a recovery message ("run focus-window or bring X forward, then retry"). The engine never auto-restores focus mid-run — `focus-window` is an explicit, traceable step.

## Input discipline

- Only `WindowsInputExecutor` calls `SendInput`; all actions serialize through one semaphore.
- Pressed modifiers are always released (`finally`), so a failed step cannot leave Ctrl/Alt/Shift stuck.
- Coordinates come only from explicit workflow values, mapped through bounds-checked helpers; out-of-bounds coordinates are rejected before any input occurs.
- Elements are checked for enabled/off-screen state before clicking.

## Cancellation

The run lease is checked at every atomic boundary: before each step and inside the input executor before each send. Escape (desktop app, while a run is active) and Ctrl+C (CLI) cancel the token; queued actions fail instead of executing. A cancelled run is reported as `Cancelled`, never as success.

## Explicit scope

- No shell or terminal execution exists anywhere in the runtime.
- Files are read for assertions and written only as run artifacts into the caller-chosen output directory.
- There is no file deletion, no privilege elevation, no UAC secure-desktop automation, and no network access.
- The recorder observes only; it never sends input.

## Determinism

- Selectors resolve against fresh UIA state each time; no stale cached elements.
- Waits poll explicit conditions with bounded timeouts; arbitrary sleeps exist only as clearly-labeled `delayMs`.
- Image assertions compare exact pixels within a declared channel delta — no fuzzy matching.
- Traces record what was expected, what was observed, and evidence screenshots, making failures reproducible and diagnosable.

## Residual risks (documented honestly)

- Desktop input is inherently time-of-check-to-time-of-use: between resolving an element's bounds and the OS delivering the click, the element can move. The engine minimizes this window (fresh resolution immediately before send, foreground re-check at send time) but cannot eliminate the race; traces record resolved bounds to make such failures diagnosable.
- PrintWindow capture may return stale frames for GPU-composited windows on some drivers; screenshot evidence should be sanity-checked when debugging.
- Weak selectors (className-only, picked multiplicity, coordinates) replay only until application layout changes; they are flagged wherever they appear.
