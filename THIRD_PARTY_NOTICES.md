# Third-party notices

PointPilot uses the following runtime dependencies under their respective licenses:

- Microsoft.Web.WebView2 — Microsoft software license terms.
- xUnit.net — Apache License 2.0 (test only).
- Microsoft.NET.Test.Sdk — MIT License (test only).
- coverlet — MIT License (test only).
- TypeScript — Apache License 2.0 (build only).

The `.agent/skills/` directory contains exact or minimally wrapped development-time instruction sets retained under their upstream licenses. Source repositories, paths, pinned commits, destinations, and retention decisions are recorded in `.agent/skills.lock`. These skills are not linked into or distributed with the PointPilot runtime package by `scripts/package.ps1`.

No GIMP code, API, plug-in, or artwork is bundled. The generated demo artwork is original programmatic output from this repository.
