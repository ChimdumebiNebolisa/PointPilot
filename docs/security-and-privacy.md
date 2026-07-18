# Security and privacy

The standard OpenAI credential is read only by the native PointPilot host from
the ignored `.env.local` file or process environment. It is never committed,
logged, rendered, or sent to a browser surface.

Microphone audio is streamed only after an explicit session start. Target-window
screenshots are created only for user-requested Teach, Guide, Act, or Verify
work. The target inspector rejects PointPilot-owned and sensitive windows before
capture; the input executor separately rejects stale revisions, target changes,
out-of-bounds coordinates, and non-GIMP mutations.

Screen/model content is untrusted data, not authorization. PointPilot keeps no
long-term memory, raw audio recordings, screenshots, or transcripts. It stops
instead of claiming success when visual verification is uncertain.

