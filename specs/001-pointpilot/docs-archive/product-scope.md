# Product scope

PointPilot is a general-purpose co-present desktop companion. Its product architecture is not coupled to GIMP: voice, state, task revision, screenshot, visual reasoning, action, overlay, policy, and verification are separate contracts and services.

The only verified actuation adapter in this release is foreground GIMP 3.x on Windows 11. Teach may inspect another foreground application on a best-effort basis, but mutation outside GIMP is disabled in code and there is no public unsafe-mode switch.

## Locked release slice

- Voice-first activation followed by continuous follow-up turns.
- Natural interruption of speech and action.
- Teach with grounded pointer overlay and no mutation.
- Guide with one contextual step per turn.
- Act through GPT-5.6 Computer Use and visible Windows input.
- Native GIMP undo for reversible edits.
- Exact confirmation and file verification for PNG export.
- The promotional-graphic hero workflow in the generated layered fixture.

## Explicit non-goals

There is no GIMP API or plug-in, hidden automation script, background cross-application agent, credential entry, shell execution, payment, publishing, permanent deletion, arbitrary application control, workflow marketplace, or claim of broad verified application support.
