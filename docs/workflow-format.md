# Workflow format (schemaVersion 1)

Workflows are YAML files. Parsing is strict: unknown keys, unknown step kinds, wrong types, and unsupported schema versions are rejected with `path: message` diagnostics before a run starts. The same model backs the desktop app, the CLI, and the recorder's drafts.

## Top level

```yaml
schemaVersion: 1          # required; only 1 is supported
name: notepad-demo        # required, non-empty
description: optional text
variables:
  message:
    default: hello        # used when not provided
  out_path:
    required: true        # must be provided by the runner (--var)
defaults:
  timeoutMs: 5000         # inherited per-step wait/action timeout
target:
  processName: Notepad    # required
  processNameMatch: exact # exact (default) | prefix
  windowTitleRegex: ".*Untitled.*"   # optional; narrows binding when several windows match
steps: [ ... ]
```

`${name}` substitution is supported in string fields (text, paths, key names, selector criteria). Referencing an undeclared variable is a validation error.

## Steps

| Kind | Fields | Notes |
|---|---|---|
| `focus-window` | — | Brings the bound window to the foreground and verifies it. |
| `focus-control` | `selector` | Sets UI Automation focus on the resolved element. |
| `click` | `selector`, optional `kind: single\|double\|right` | Clicks the element center after enabled/visible checks. |
| `double-click`, `right-click` | `selector` | Readable aliases for click kinds. |
| `type-text` | `text`, optional `selector` | Types into the focused control, or focuses the selector target first. |
| `press` | `keys: [CTRL, S]` | 1–8 keys; Windows key names (`KeyNormalizer` maps common aliases). |
| `wait` | `until:` condition | See below. |
| `screenshot` | — | Captures the bound window as diagnostic evidence in the trace directory. |
| `assert-file` | `path`, `condition: exists\|not-exists` | First-class postcondition. |
| `assert-window` | `condition: visible\|minimized\|closed\|foreground` | Window-state postcondition. |
| `assert-control` | `selector`, `state: exists\|visible\|enabled\|value`, `value` (for `value`) | UIA state/value postcondition; value compares exactly. |
| `assert-image` | `selector`, `referenceImage`, `maxChannelDelta` | Deterministic pixel parity of the element region against a committed PNG. |

Every step accepts optional `name:` (appears in traces) and `timeoutMs:` (overrides `defaults.timeoutMs`).

### Wait conditions

```yaml
- step: wait
  until: { windowTitleRegex: "Saved.*" }
- step: wait
  until:
    control: { automationId: statusLabel }
    state: visible
- step: wait
  until: { file: "C:/out/result.png", fileCondition: exists }
- step: wait
  until: { delayMs: 500 }   # explicit bounded sleep (1..60000); prefer signal waits
```

## Selectors

```yaml
selector: { automationId: saveButton }            # preferred
selector: { name: Save, role: button }            # accessible name + optional control type
selector: { className: ToolButton, role: button } # weaker; flagged weak in traces
selector: { automationId: row, pick: index:2 }    # declared multiplicity; flagged weak
selector: { x: 120, y: 40 }                       # explicit coordinates; last resort; flagged weak
```

Rules:

- All provided criteria must match simultaneously (exact, case-insensitive).
- Resolution collects **all** matches in the bound window subtree. Zero matches fail with the searched-element count; multiple matches fail unless `pick:` is declared.
- Coordinate selectors are window-relative, rejected outside live bounds, never combined with property criteria, and always recorded as weak targets.

See [selectors](selectors.md) for resolution semantics and [the safety model](safety-model.md) for how targets are re-validated at action time.

## Complete example

See [`examples/notepad-demo.yaml`](../examples/notepad-demo.yaml).
