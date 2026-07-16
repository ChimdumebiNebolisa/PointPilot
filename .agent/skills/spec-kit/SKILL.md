---
name: spec-kit
description: Use the pinned GitHub Spec Kit templates to turn the PointPilot PRD into traceable specifications, plans, checklists, and executable tasks without replacing or expanding the PRD.
license: MIT
---

# PointPilot Spec Kit workflow

Treat `PointPilot_PRD.md` as the authoritative product source. Use the templates in `references/` selectively:

1. Translate requirements into independently testable user journeys and acceptance scenarios.
2. Mark real uncertainty explicitly; do not invent missing product scope.
3. Trace implementation decisions and tasks to PRD acceptance criteria.
4. Keep architecture and code details in the plan, not the user-facing specification.
5. Define verification beside every implementation task.
6. Preserve the PRD's locked distinction between general-purpose architecture and GIMP as the first verified actuation environment.

The upstream CLI and scripts are intentionally not installed. This project-local copy contains only reviewed declarative templates.

