# PointPilot implementation specification

**Source:** `PointPilot_PRD.md`
**Status:** Implemented; live acceptance pending
**Created:** 2026-07-15

This specification translates the PRD into testable implementation slices. If it conflicts with the PRD, the PRD wins.

## Success predicate

On the documented Windows 11 single-monitor environment, an explicitly activated PointPilot session remains voice-available, grounds teach and guide responses in the foreground window, runs GPT-5.6 Computer Use actions only through a locally guarded GIMP executor, invalidates stale work on speech or Escape, requires revision-bound confirmation for consequential file actions, and reports success only after visual and deterministic verification.

## Non-counting outcomes

- A static companion or prerecorded demo.
- Text chat presented as continuous Realtime voice.
- Computer actions not produced through the Responses computer tool.
- GIMP-specific core types or a hidden GIMP API/plugin/script.
- Fake integrations presented as live.
- Old-plan input executing after interruption.
- A claimed success based only on clicking a control.
- Export without exact-path confirmation and file verification.
- Broad claims that applications outside GIMP are verified for actuation.

## User journeys

### P1 - Continuous co-present session

Given an inactive app, starting a session connects Realtime, displays microphone/listening state, accepts at least five turns without reactivation, mirrors spoken responses as text, and returns to listening after speech.

### P1 - Interruptible guarded action

Given foreground GIMP and an active task, Computer Use actions execute serially only while the task lease, allowlisted foreground HWND, coordinates, and confirmation remain valid. Speech-start or Escape invalidates the lease before the next action.

### P1 - Verified GIMP hero workflow

Given the pinned fixture, PointPilot teaches the layers panel, guides one verified step, switches to Act without losing context, applies the requested edit visibly, honors the changed constraint, changes the subtitle, confirms export, and verifies the PNG.

### P2 - Safe recovery and undo

Integration failures enter explicit safe error states. Native GIMP undo is available for supported reversible edits and requires visual verification.

## Acceptance traceability

| PRD criterion | Implementation evidence | Verification |
|---|---|---|
| AC-01 | solution, setup, packaged build | restore/build/start smoke |
| AC-02-04 | Realtime WebRTC surface and session bridge | TS checks, fake events, live voice |
| AC-05-06 | task leases, interruption and global Escape | unit/integration tests, live stop |
| AC-07-09 | teach/guide/act coordinator and overlay | integration tests, live GIMP |
| AC-10 | Responses computer loop plus Win32 executor | fake loop and live hero workflow |
| AC-11-12 | HWND/process and coordinate gates | unit/integration tests |
| AC-13 | verification-required completion | integration tests |
| AC-14-15 | revision-bound export confirmation and file check | unit/integration/live export |
| AC-16 | native undo goal and visible verification | integration/live GIMP |
| AC-17 | HWND-targeted capture and separate overlay window | architecture test/live screenshot |
| AC-18-19 | error mapper, secret boundary, redaction | tests and secret scan |
| AC-20 | pinned fixture and run ledger | three consecutive live runs |

## Assumptions and unknowns

- .NET 8 is the verified local target; .NET 10 is a documented upgrade path.
- `gpt-5.6` and `gpt-realtime-2.1` are configurable defaults based on current official docs and may be unavailable to some projects.
- GIMP 3 is not installed on the implementation machine, so live GIMP evidence requires the documented demo machine.
