# Selector semantics

## Model

A `SelectorSpec` is an explicit union: property criteria (`automationId`, `name`, `className`, `role`), an optional declared `pick`, or raw window-relative coordinates. There is no silent fallback ordering — the workflow author states exactly one selector per step, and the engine resolves it deterministically.

Preference guidance (enforced by trace flagging, not by guessing):

1. `automationId` — stable across runs; strongest.
2. `name` (+ optional `role`) — accessible name; strong when unique.
3. `className`/`role` combinations — structural; **flagged weak** because they usually match many elements.
4. Declared `pick` — makes multiplicity explicit; **flagged weak**.
5. Coordinates — last resort; validated in-bounds; **flagged weak**.

Weakness is surfaced in three places: the workflow inspection list in the desktop app, the CLI `validate` output, and each step's `resolved.weakTarget` field in traces.

## Resolution algorithm

For every action or assertion:

1. The bound window's UIA subtree is enumerated fresh (no cached element references survive between steps).
2. All elements whose properties match every provided criterion (exact, case-insensitive) are collected.
3. Zero matches → `SelectorFailureException` including the criteria and how many elements were searched.
4. Multiple matches without `pick` → failure listing the match count.
5. With `pick` → `first` or `index:<n>` selects from the ordered matches; out-of-range picks fail.
6. The resolved element's identity (automation ID, name, class, control type) and bounds are recorded in the trace.

## Failure behavior

Selectors never guess. Ambiguity, absence, off-screen elements, and disabled elements all fail closed with actionable messages, for example:

- `Selector {name=Save} matched 3 elements; refine the selector or declare an explicit pick.`
- `Selector {automationId=ok} matched no elements in the target window. Searched 214 elements.`

## Image targeting

Deterministic image matching exists **only** as `assert-image`: exact per-channel pixel comparison of a captured element region against a committed reference PNG within `maxChannelDelta`. It verifies state; it never locates targets. Size mismatches between capture and reference fail rather than fuzzy-compare.

## Known limits

- Controls without UIA exposure (owner-drawn canvases) cannot be selected by properties; use coordinates deliberately or assert by image.
- Automation IDs vary between applications and versions; recorder drafts mark steps that rely on weaker criteria so they can be reviewed before replay.
