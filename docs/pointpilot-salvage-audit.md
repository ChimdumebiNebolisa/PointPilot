# PointPilot salvage audit

The archived WPF implementation remains available at
`pointpilot-wpf-archive`. This rewrite retains product intent, not the old
runtime implementation.

| Candidate from archive | Classification | Use in Python/PyQt rewrite |
|---|---|---|
| Product name, copy, README language | COPY DOCUMENTATION | Rename the app and present general-purpose screen help with honest GIMP-only control. |
| PRD and product scope | COPY DOCUMENTATION | Preserve the verified-environment and safety limits. |
| Demo fixture and generator | COPY ASSET | Retain the `.ora` fixture and adapt the generator documentation as needed. |
| Demo script and GIMP scenarios | COPY DOCUMENTATION | Use as the focused manual workflow. |
| Task revisions and stale-action rejection | REIMPLEMENT | Implement in native Python task contracts. |
| Escape and interruption ordering | REIMPLEMENT | Make one cancellation path for speech, action, and emergency stop. |
| Confirmation and sensitive-window rules | REIMPLEMENT | Enforce locally before input/capture. |
| Tool prompts and verification semantics | REIMPLEMENT | Keep model output untrusted and require visible/file evidence. |
| UI/UX audit conclusions | COPY DOCUMENTATION | Apply its compact, focus-preserving findings to PyQt. |
| C#/WPF/WebView2 code | REJECT | It is not a useful runtime dependency for the new foundation. |
| Synthetic acceptance harness | REJECT | Manual-first gates take precedence for this rewrite. |
| .NET build/package graph | REJECT | Replace with Python tests and packaging after manual viability. |

