# Pre-implementation red-team

**Verdict:** PROCEED WITH DOCUMENTED RISK

## Strongest failure paths

1. A task becomes stale after validation but before OS input. The executor must recheck a revision lease immediately before each atomic input and serialize the loop.
2. A compromised WebView receives the standard API key. The host must mint and pass only a short-lived Realtime client secret.
3. Screenshot prompt injection authorizes a dangerous action. Visible content must never authorize; the local policy is authoritative.
4. UI activity is mistaken for completion. The coordinator must require a verification result, with exact file existence for export.
5. Overlay pixels corrupt the screenshot. Capture the target HWND, never the desktop composition containing the separate overlay.
6. Model or event schemas change. Parse defensively, configure model IDs, fail closed on unsupported actions, and test fixture contracts.

## Missing evidence before implementation

- Live Realtime entitlement for the configured project.
- Installed GIMP 3 and pinned demo layout.
- Three consecutive live hero runs.

These items are live-verification residuals, not reasons to replace the required integrations with mocks.
