# Migration note: removal of the model-driven runtime

PointPilot 1.0 (tag `pointpilot-wpf-archive`, commit `631b64e`) was a voice-first AI companion: continuous OpenAI Realtime speech, GPT-5.6 visual grounding and Computer Use acting on foreground GIMP through model-chosen screen coordinates. This repository no longer contains that product. The rebuild replaced it with the deterministic workflow recorder/replayer described in this documentation.

## Removed from the runtime

- The OpenAI integration layer (`OpenAiHttp`, `OpenAiOptions`, `ComputerUseService`, `OpenAiVisualReasoningService`, `RealtimeTokenService`) and all credential handling. There are no environment variables, no API keys, and no `.env` files anymore.
- The WebView2 Realtime WebRTC surface and the entire Node/TypeScript toolchain (`package.json`, web client, npm scripts). CI and packaging no longer use Node.
- Model-driven orchestration (`PointPilotWorkflow`), keyword-based goal safety classification (`ActionPolicy.ClassifyGoal`), vision-model verification (`VerificationService`), the voice-session state machine, Teach/Guide/Act tools, tray voice session controls, and the OpenAI smoke script.
- The GIMP demo fixture generator and binary fixture; the hardcoded GIMP process allowlist in core policy was replaced by per-workflow declared targets.
- The old acceptance/live-demo documents moved to `specs/001-pointpilot/docs-archive/` as historical reference.

## Preserved and reworked

- Coordinate/bounds primitives and key normalization (now with window-relative mapping).
- The lease/revision/cancellation discipline (simplified to `RunController`; serialized SendInput executor with modifier release guarantees).
- PrintWindow capture (generalized to arbitrary HWNDs with clip regions) and the non-activating overlay technique.
- Deterministic packaging and CI structure, locked restores, warnings-as-errors.

## Replacing capabilities

| Old (model-driven) | New (deterministic) |
|---|---|
| Vision-chosen screen coordinates | UI Automation selectors with uniqueness checks |
| Natural-language goal classification | Explicit steps authored in versioned YAML |
| Vision-model result verification | First-class file/window/control/image assertions |
| Speech interruption | Cancellation at every atomic input boundary |
| GIMP allowlist in core | Per-workflow target declaration |

The baseline audit and design records for this transition live in `specs/002-deterministic-rebuild/`.
