# Live manual gates

Run these only with a microphone, GIMP 3.x, and the PointPilot dependency set
installed. Record PASS, FAIL, or BLOCKED with a short observation.

| Gate | Result | Observation |
|---|---|---|
| Start session and identify foreground GIMP | BLOCKED | Host cannot complete the isolated PyQt dependency installation. |
| Point to Layers panel | BLOCKED | Requires a live PyQt and OpenAI session. |
| Point to Product layer | BLOCKED | Requires live target/UI grounding. |
| Click Product layer | BLOCKED | Requires live GIMP actuation evidence. |
| Barge in during speech | BLOCKED | Requires microphone evidence. |
| Barge in during action | BLOCKED | Requires live GIMP and microphone evidence. |
| Escape emergency stop | BLOCKED | Requires live global-hook evidence. |

A PASS requires visible pointer evidence, correct cursor movement/click, no
self-targeting, no stale post-interruption action, and no post-Escape input.

