# Third-party notices

PointPilot uses the following runtime dependencies under their respective licenses:

- YamlDotNet — MIT License (workflow parsing and emission).
- xUnit.net — Apache License 2.0 (test only).
- Microsoft.NET.Test.Sdk — MIT License (test only).
- coverlet — MIT License (test only).

The `.agent/skills/` directory contains exact or minimally wrapped development-time instruction sets retained under their upstream licenses. Source repositories, paths, pinned commits, destinations, and retention decisions are recorded in `.agent/skills.lock`. These skills are not linked into or distributed with the PointPilot runtime package by `scripts/package.ps1`.

PointPilot's runtime links only Microsoft platform libraries (.NET, WPF, WinForms) plus YamlDotNet. No model-provider SDKs, WebView2, or Node.js tooling is referenced.
