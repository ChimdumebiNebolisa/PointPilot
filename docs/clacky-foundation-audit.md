# Clacky foundation audit

Source: `Raynan00/clacky` at `e239089a4eb9daf7ac62d0f5c38e92fa53648499`.

## Baseline environment result

The untouched imported source was started with Python 3.12 from
`clacky/shell/main.py`. It entered a long-running process rather than returning
an import/configuration error, but the bounded host command terminated it before
a targetable window or tray surface could be inspected. The isolated dependency
installation was also terminated by the host before its full completion.

Therefore no manual behavior below is marked as working. `Not verified` is
evidence of an unavailable baseline observation, not a product failure or a
README-derived claim. Re-run this table on a workstation where the original
process can remain open and a human can provide microphone input before treating
any Clacky capability as live evidence.

| Subsystem | Works now | Required by PointPilot | Keep | Modify | Remove | Evidence |
|---|---:|---:|---:|---:|---:|---|
| Application manager | — | Yes | Yes | Yes | No | Source inspected; live UI unavailable in bounded host run. |
| State machine | — | Yes | Yes | Yes | No | Source inspected; live UI unavailable. |
| Tray | — | Yes | Yes | Yes | No | Original process did not expose a targetable tray surface before termination. |
| Panel | — | Yes | Yes | Yes | No | Original process did not expose a targetable window before termination. |
| Overlay | — | Yes | Yes | Yes | No | No live visual inspection completed. |
| Target tracking | — | Yes | Yes | Yes | No | Static source inspection only. |
| Screen capture | — | Yes | Yes | Yes | No | Static source inspection only. |
| UI Automation | — | Yes | Yes | Yes | No | Static source inspection only. |
| Computer actuation | — | Yes | No | Yes | No | `ComputerAgent.run` raises `NotImplementedError`; no live action was observed. |
| Permissions | — | Yes | No | Yes | No | Classifier is scaffold code, not live end-to-end evidence. |
| Voice input | — | Yes | No | Replace | Yes | No physical microphone test was possible. |
| Speech output | — | Yes | No | Replace | Yes | No live playback test was possible. |
| Provider routing | — | No | No | No | Yes | Static source shows multi-provider routing. |
| Background tasks | — | No | No | No | Yes | Static source shows Hermes/background facilities. |
| Integrations | — | No | No | No | Yes | Static source shows Google, MCP, Composio paths. |
| Routines | — | No | No | No | Yes | Static source shows skills/routines. |
| Memory | — | No | No | No | Yes | Static source shows persistent memory/journal paths. |
| Recovery | — | Yes | No | Yes | No | Static source inspected; live sleep/audio recovery unverified. |
| Logging | — | Yes | No | Yes | No | Static source inspected; PointPilot will retain metadata-only logging. |
| Packaging | — | Yes | No | Yes | No | Existing PyInstaller packaging references Clacky and broad dependencies. |
| Focus preservation | — | Yes | Yes | Yes | No | No live focus inspection completed. |
| Escape cancellation | — | Yes | Yes | Yes | No | No live key test completed. |

## Follow-up manual gates

When an interactive baseline machine is available, record timestamped results
for startup, tray behavior, foreground recognition, target-only capture,
pointing, overlay focus behavior, mouse movement/clicking, Escape, voice input,
panel behavior, and focus preservation. Do not overwrite this blocked-run
evidence; append the live results.

