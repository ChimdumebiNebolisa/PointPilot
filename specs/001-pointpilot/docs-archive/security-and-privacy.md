# Security and privacy

## Data sent to OpenAI

During an active session, microphone audio is sent over WebRTC to the configured Realtime model. On Teach/Guide/Act/Verify turns, a PNG capture of the foreground target window and the user’s task text are sent to the configured Responses model. Computer Use receives subsequent target-window screenshots until it stops. PointPilot does not intentionally capture the desktop, other windows, or its own overlay.

PointPilot stores no audio or screenshots. The only expected local output is the user-confirmed export path and normal build/runtime files. OpenAI retention and data controls depend on the API project configuration; operators must review their organization policy before use with sensitive data.

## Credentials

The standard API key is read by the .NET host from the process environment or ignored `.env.local`. It is never bundled into JavaScript, rendered, logged, or sent to GIMP. WebView2 receives only a short-lived Realtime client secret in memory. `.env*` is ignored except `.env.example`.

## Trust boundaries and prompt injection

Visible screen content is untrusted model input and cannot grant permission. Only direct user speech plus local policy may authorize an action. The executor cannot open a shell, cross to another process, or mutate outside foreground GIMP. Export/save-like goals require exact confirmation; password, payment, credential, terminal, publishing, permanent deletion, and external-send goals are prohibited.

## Residual risk

Visible UI automation can partially change a document before an interruption, model/API error, unexpected dialog, or uncertain verification. The UI explicitly tells the user to inspect GIMP and use native undo where appropriate. Export and overwrite are not promised reversible. `PrintWindow` fidelity and Computer Use behavior must be revalidated on every pinned environment change.

Report security issues privately to the repository owner. Do not attach API keys, screenshots containing private data, or exported customer material to a public issue.
