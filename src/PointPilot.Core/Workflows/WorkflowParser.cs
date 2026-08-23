using System.Security.Cryptography;
using System.Text;
using PointPilot.Core.Selectors;
using YamlDotNet.RepresentationModel;

namespace PointPilot.Core.Workflows;

public sealed record WorkflowDiagnostic(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}

public sealed record WorkflowParseResult(WorkflowDefinition? Definition, IReadOnlyList<WorkflowDiagnostic> Diagnostics)
{
    public bool Success => Definition is not null && Diagnostics.Count == 0;
}

/// <summary>
/// Strict YAML parser for workflow schemaVersion 1. Unknown keys, unknown step kinds,
/// wrong types, and unsupported schema versions are rejected with actionable, line-numbered
/// diagnostics instead of being silently ignored. Semantic validation (regex compilation,
/// timeout positivity, variable references) runs after variable substitution.
/// </summary>
public static partial class WorkflowParser
{
    private const int SupportedSchemaVersion = 1;

    public static WorkflowParseResult Parse(string yamlText, string sourcePath)
    {
        var diagnostics = new List<WorkflowDiagnostic>();
        byte[] hashBytes;
        try { hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(yamlText)); }
        catch (ArgumentNullException) { hashBytes = []; }
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        YamlMappingNode root;
        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yamlText));
            if (stream.Documents.Count != 1)
            {
                diagnostics.Add(new("document", $"Expected exactly one YAML document, found {stream.Documents.Count}."));
                return new(null, diagnostics);
            }
            if (stream.Documents[0].RootNode is not YamlMappingNode mapping)
            {
                diagnostics.Add(new("document", "The workflow root must be a YAML mapping."));
                return new(null, diagnostics);
            }
            root = mapping;
        }
        catch (Exception ex) when (ex is YamlDotNet.Core.YamlException or ArgumentException)
        {
            var at = ex is YamlDotNet.Core.YamlException ye ? $" (line {ye.Start.Line})" : "";
            diagnostics.Add(new("document", $"The workflow is not valid YAML{at}: {ex.Message}"));
            return new(null, diagnostics);
        }

        RejectUnknownKeys(root, ["schemaVersion", "name", "description", "variables", "defaults", "target", "steps"], "workflow", diagnostics);

        var schema = GetScalar(root, "schemaVersion");
        if (!int.TryParse(schema, out var schemaVersion) || schemaVersion != SupportedSchemaVersion)
        {
            diagnostics.Add(new("schemaVersion", $"Unsupported workflow schema version '{schema}'. This build supports only version {SupportedSchemaVersion}."));
            return new(null, diagnostics);
        }

        var name = GetString(root, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            diagnostics.Add(new("name", "A non-empty workflow name is required."));
            return new(null, diagnostics);
        }

        var description = GetString(root, "description");

        var variables = new List<VariableSpec>();
        if (root.Children.TryGetValue(new YamlScalarNode("variables"), out var variablesNode))
        {
            if (variablesNode is not YamlMappingNode variablesMap)
                diagnostics.Add(new("variables", "variables must be a mapping of name to definition."));
            else
                foreach (var entry in variablesMap.Children)
                {
                    var variableName = ((YamlScalarNode)entry.Key).Value ?? "";
                    if (entry.Value is YamlScalarNode shorthand)
                        variables.Add(new(variableName, Required: false, Default: shorthand.Value));
                    else if (entry.Value is YamlMappingNode specMap)
                    {
                        var required = GetBool(specMap, "required") ?? false;
                        var def = GetString(specMap, "default");
                        if (required && def is not null)
                            diagnostics.Add(new($"variables.{variableName}", "A variable cannot be both required and have a default."));
                        variables.Add(new(variableName, required, def));
                    }
                    else diagnostics.Add(new($"variables.{variableName}", "A variable must be a scalar default or a mapping with required/default fields."));
                }
        }

        var defaultsTimeout = 5000;
        if (root.Children.TryGetValue(new YamlScalarNode("defaults"), out var defaultsNode))
        {
            if (defaultsNode is not YamlMappingNode defaultsMap)
                diagnostics.Add(new("defaults", "defaults must be a mapping containing timeoutMs."));
            else
            {
                RejectUnknownKeys(defaultsMap, ["timeoutMs"], "defaults", diagnostics);
                if (GetInt(defaultsMap, "timeoutMs") is { } t)
                {
                    if (t <= 0) diagnostics.Add(new("defaults.timeoutMs", "timeoutMs must be a positive number of milliseconds."));
                    else defaultsTimeout = t;
                }
            }
        }

        TargetSpec? target = null;
        if (root.Children.TryGetValue(new YamlScalarNode("target"), out var targetNode) && targetNode is YamlMappingNode targetMap)
        {
            RejectUnknownKeys(targetMap, ["processName", "processNameMatch", "windowTitleRegex"], "target", diagnostics);
            var processName = GetString(targetMap, "processName");
            if (string.IsNullOrWhiteSpace(processName)) diagnostics.Add(new("target.processName", "A target processName is required."));
            else target = new(processName.Trim(), ParseMatchMode(GetString(targetMap, "processNameMatch")), GetString(targetMap, "windowTitleRegex"));
        }
        else diagnostics.Add(new("target", "A target section with processName is required."));

        var steps = new List<StepSpec>();
        if (root.Children.TryGetValue(new YamlScalarNode("steps"), out var stepsNode))
        {
            if (stepsNode is not YamlSequenceNode stepsSeq)
                diagnostics.Add(new("steps", "steps must be a list."));
            else if (stepsSeq.Children.Count == 0)
                diagnostics.Add(new("steps", "A workflow must contain at least one step."));
            else
                for (var i = 0; i < stepsSeq.Children.Count; i++)
                {
                    if (stepsSeq.Children[i] is not YamlMappingNode stepMap)
                    {
                        diagnostics.Add(new($"steps[{i}]", "Each step must be a mapping with a 'step' kind key."));
                        continue;
                    }
                    ParseStep(stepMap, i, diagnostics, steps);
                }
        }
        else diagnostics.Add(new("steps", "A steps list is required."));

        if (target is null || diagnostics.Count > 0)
            return new(null, diagnostics);

        var definition = new WorkflowDefinition(schemaVersion, name.Trim(), description, variables, target, new DefaultsSpec(defaultsTimeout), steps, sourcePath, hash);
        var semantic = ValidateSemantics(definition);
        diagnostics.AddRange(semantic);
        return semantic.Length == 0 ? new(definition, []) : new(null, diagnostics);
    }

    private static void ParseStep(YamlMappingNode stepMap, int index, List<WorkflowDiagnostic> diagnostics, List<StepSpec> steps)
    {
        var path = $"steps[{index}]";
        var kindRaw = GetScalar(stepMap, "step");
        if (string.IsNullOrWhiteSpace(kindRaw))
        {
            diagnostics.Add(new(path, "Every step requires a 'step' kind key (for example: click, type-text, press)."));
            return;
        }
        var kind = kindRaw.Trim().ToLowerInvariant();
        var name = GetString(stepMap, "name");
        int? timeout = GetInt(stepMap, "timeoutMs") is { } t ? (t > 0 ? t : null) : null;
        if (GetInt(stepMap, "timeoutMs") is { } rawTimeout && rawTimeout <= 0)
            diagnostics.Add(new($"{path}.timeoutMs", "timeoutMs must be positive."));

        switch (kind)
        {
            case "focus-window":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs"], path, diagnostics);
                steps.Add(new FocusWindowStep(name, timeout));
                break;
            case "click" or "double-click" or "right-click":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs", "selector", "kind"], path, diagnostics);
                var clickKind = kind switch
                {
                    "double-click" => ClickKind.Double,
                    "right-click" => ClickKind.Right,
                    _ => ParseClickKind(GetString(stepMap, "kind"))
                };
                if (GetString(stepMap, "kind") is { } k && k is not ("single" or "double" or "right"))
                    diagnostics.Add(new($"{path}.kind", $"'{k}' is not a click kind; use single, double, or right."));
                if (TryParseSelector(stepMap, path, diagnostics, out var clickSelector))
                    steps.Add(new ClickStep(clickSelector!, clickKind, name, timeout));
                break;
            case "type-text":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs", "text", "selector"], path, diagnostics);
                var text = GetString(stepMap, "text");
                if (string.IsNullOrEmpty(text)) diagnostics.Add(new($"{path}.text", "type-text requires non-empty text."));
                TryParseOptionalSelector(stepMap, path, diagnostics, out var typeSelector);
                steps.Add(new TypeTextStep(text ?? "", typeSelector, name, timeout));
                break;
            case "press":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs", "keys"], path, diagnostics);
                if (stepMap.Children.TryGetValue(new YamlScalarNode("keys"), out var keysNode) && keysNode is YamlSequenceNode keysSeq)
                {
                    var keys = keysSeq.Children.Select(c => c.ToString()).Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).ToList();
                    if (keys.Count is < 1 or > 8) diagnostics.Add(new($"{path}.keys", "press requires between 1 and 8 keys."));
                    else steps.Add(new PressStep(keys, name, timeout));
                }
                else diagnostics.Add(new($"{path}.keys", "press requires a list of key names, e.g. [CTRL, S]."));
                break;
            case "focus-control":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs", "selector"], path, diagnostics);
                if (TryParseSelector(stepMap, path, diagnostics, out var focusSelector))
                    steps.Add(new FocusControlStep(focusSelector!, name, timeout));
                break;
            case "wait":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs", "until"], path, diagnostics);
                if (stepMap.Children.TryGetValue(new YamlScalarNode("until"), out var untilNode) && untilNode is YamlMappingNode untilMap)
                {
                    if (ParseWaitCondition(untilMap, $"{path}.until", diagnostics) is { } condition)
                        steps.Add(new WaitStep(condition, name, timeout));
                }
                else diagnostics.Add(new($"{path}.until", "wait requires an 'until' condition mapping."));
                break;
            case "screenshot":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs"], path, diagnostics);
                steps.Add(new ScreenshotStep(name, timeout));
                break;
            case "assert-file":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs", "path", "condition"], path, diagnostics);
                var filePath = GetString(stepMap, "path");
                if (string.IsNullOrWhiteSpace(filePath)) { diagnostics.Add(new($"{path}.path", "assert-file requires a path.")); break; }
                steps.Add(new AssertFileStep(filePath.Trim(), ParseFileCondition(GetString(stepMap, "condition")), name, timeout));
                break;
            case "assert-window":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs", "condition"], path, diagnostics);
                steps.Add(new AssertWindowStep(ParseWindowCondition(GetString(stepMap, "condition")), name, timeout));
                break;
            case "assert-control":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs", "selector", "state", "value"], path, diagnostics);
                if (TryParseSelector(stepMap, path, diagnostics, out var assertSelector))
                {
                    var state = ParseControlState(GetString(stepMap, "state"));
                    var value = GetString(stepMap, "value");
                    if (state == ControlState.Value && value is null)
                        diagnostics.Add(new($"{path}.value", "state 'value' requires a 'value' field to compare against."));
                    steps.Add(new AssertControlStep(assertSelector!, state, value, name, timeout));
                }
                break;
            case "assert-image":
                RejectUnknownKeys(stepMap, ["step", "name", "timeoutMs", "selector", "referenceImage", "maxChannelDelta"], path, diagnostics);
                if (TryParseSelector(stepMap, path, diagnostics, out var imageSelector))
                {
                    var reference = GetString(stepMap, "referenceImage");
                    if (string.IsNullOrWhiteSpace(reference)) { diagnostics.Add(new($"{path}.referenceImage", "assert-image requires a referenceImage path.")); break; }
                    steps.Add(new AssertImageStep(imageSelector!, reference!.Trim(), GetInt(stepMap, "maxChannelDelta") ?? 8, name, timeout));
                }
                break;
            default:
                diagnostics.Add(new(path, $"'{kind}' is not a known step kind. Known kinds: focus-window, focus-control, click, double-click, right-click, type-text, press, wait, screenshot, assert-file, assert-window, assert-control, assert-image."));
                break;
        }
    }

    private static WaitCondition? ParseWaitCondition(YamlMappingNode map, string path, List<WorkflowDiagnostic> diagnostics)
    {
        RejectUnknownKeys(map, ["windowTitleRegex", "control", "state", "file", "fileCondition", "delayMs"], path, diagnostics);
        if (map.Children.ContainsKey(new YamlScalarNode("windowTitleRegex")))
            return new WaitForWindowTitle(GetString(map, "windowTitleRegex") ?? "");
        if (map.Children.TryGetValue(new YamlScalarNode("control"), out var controlNode) && controlNode is YamlMappingNode controlMap)
        {
            var controlSelector = ParseSelectorNode(controlMap, path + ".control", diagnostics);
            if (controlSelector is not null)
                return new WaitForControl(controlSelector, ParseControlState(GetString(map, "state")));
            return null;
        }
        if (map.Children.ContainsKey(new YamlScalarNode("file")))
            return new WaitForFile((GetString(map, "file") ?? "").Trim(), ParseFileCondition(GetString(map, "fileCondition")));
        if (map.Children.TryGetValue(new YamlScalarNode("delayMs"), out var delayNode))
        {
            if (int.TryParse(delayNode.ToString(), out var ms) && ms is > 0 and <= 60_000) return new DelayMs(ms);
            diagnostics.Add(new(path + ".delayMs", "delayMs must be between 1 and 60000. Prefer signal-based waits where possible."));
            return null;
        }
        diagnostics.Add(new(path, "An until condition requires one of windowTitleRegex, control, file, or delayMs."));
        return null;
    }

    private static SelectorSpec? ParseSelectorNode(YamlMappingNode map, string path, List<WorkflowDiagnostic> diagnostics) =>
        TryParseSelector(map, path, diagnostics, out var selector) ? selector : null;

    private static bool TryParseSelector(YamlMappingNode stepMap, string path, List<WorkflowDiagnostic> diagnostics, out SelectorSpec? selector)
    {
        selector = null;
        if (!stepMap.Children.TryGetValue(new YamlScalarNode("selector"), out var selectorNode) || selectorNode is not YamlMappingNode selectorMap)
        {
            diagnostics.Add(new($"{path}.selector", "This step requires a selector mapping (automationId, name, className, role, pick, or x/y coordinates)."));
            return false;
        }
        RejectUnknownKeys(selectorMap, ["automationId", "name", "className", "role", "pick", "x", "y"], $"{path}.selector", diagnostics);

        var automationId = GetString(selectorMap, "automationId");
        var elementName = GetString(selectorMap, "name");
        var className = GetString(selectorMap, "className");
        var role = GetString(selectorMap, "role");
        var pick = GetString(selectorMap, "pick");
        int? x = null, y = null;
        if (selectorMap.Children.TryGetValue(new YamlScalarNode("x"), out var xNode)) x = int.TryParse(xNode.ToString(), out var xv) ? xv : null;
        if (selectorMap.Children.TryGetValue(new YamlScalarNode("y"), out var yNode)) y = int.TryParse(yNode.ToString(), out var yv) ? yv : null;

        var hasCriteria = automationId is not null || elementName is not null || className is not null || role is not null;
        var hasCoordinates = x.HasValue || y.HasValue;

        if (hasCoordinates)
        {
            if (hasCriteria || pick is not null)
                diagnostics.Add(new($"{path}.selector", "Coordinate selectors cannot be combined with property criteria or pick."));
            if (x is null || y is null || x < 0 || y < 0)
                diagnostics.Add(new($"{path}.selector", "Coordinate selectors require non-negative x and y relative to the target window."));
            else selector = new SelectorSpec(X: x, Y: y);
            return selector is not null;
        }
        if (!hasCriteria)
        {
            diagnostics.Add(new($"{path}.selector", "Selectors need at least one of automationId, name, className, role — or explicit x/y coordinates as a last resort."));
            return false;
        }
        if (pick is not null && !IsValidPick(pick))
        {
            diagnostics.Add(new($"{path}.selector.pick", $"'{pick}' is not a valid pick; use 'first' or 'index:<number>'."));
            return false;
        }
        selector = new SelectorSpec(automationId, elementName, className, role, pick);
        return true;
    }

    private static bool TryParseOptionalSelector(YamlMappingNode stepMap, string path, List<WorkflowDiagnostic> diagnostics, out SelectorSpec? selector)
    {
        selector = null;
        if (!stepMap.Children.ContainsKey(new YamlScalarNode("selector"))) return true;
        return TryParseSelector(stepMap, path, diagnostics, out selector);
    }

    private static WorkflowDiagnostic[] ValidateSemantics(WorkflowDefinition definition)
    {
        var diagnostics = new List<WorkflowDiagnostic>();
        var declared = definition.Variables.Select(v => v.Name).ToHashSet(StringComparer.Ordinal);
        var duplicate = definition.Variables.GroupBy(v => v.Name, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) diagnostics.Add(new("variables", $"Variable '{duplicate.Key}' is declared more than once."));

        void Check(string value, string path)
        {
            foreach (System.Text.RegularExpressions.Match match in VariableSubstituter.Pattern().Matches(value))
                if (!declared.Contains(match.Groups[1].Value))
                    diagnostics.Add(new(path, $"Reference to undeclared variable '${{{match.Groups[1].Value}}}'. Declare it under variables."));
        }

        void CheckSelector(string path, SelectorSpec selector)
        {
            Check(selector.AutomationId ?? "", $"{path}.selector.automationId");
            Check(selector.Name ?? "", $"{path}.selector.name");
            Check(selector.ClassName ?? "", $"{path}.selector.className");
            Check(selector.Role ?? "", $"{path}.selector.role");
        }

        foreach (var step in definition.Steps)
        {
            switch (step)
            {
                case TypeTextStep s:
                    Check(s.Text, "steps[type-text].text");
                    if (s.Selector is not null) CheckSelector("steps[type-text]", s.Selector);
                    break;
                case ClickStep s: CheckSelector("steps[click]", s.Selector); break;
                case FocusControlStep s: CheckSelector("steps[focus-control]", s.Selector); break;
                case PressStep s:
                    for (var i = 0; i < s.Keys.Count; i++) Check(s.Keys[i], $"steps[press].keys[{i}]");
                    break;
                case WaitStep s:
                    switch (s.Condition)
                    {
                        case WaitForWindowTitle w:
                            Check(w.Regex, "steps[wait].until.windowTitleRegex");
                            try { _ = new System.Text.RegularExpressions.Regex(w.Regex); }
                            catch (ArgumentException ex) { diagnostics.Add(new("steps[wait].until.windowTitleRegex", $"Invalid regular expression: {ex.Message}")); }
                            break;
                        case WaitForControl w: CheckSelector("steps[wait].until", w.Selector); break;
                        case WaitForFile w: Check(w.Path, "steps[wait].until.file"); break;
                    }
                    break;
                case AssertFileStep s: Check(s.Path, "steps[assert-file].path"); break;
                case AssertControlStep s:
                    CheckSelector("steps[assert-control]", s.Selector);
                    if (s.Value is not null) Check(s.Value, "steps[assert-control].value");
                    break;
                case AssertImageStep s:
                    CheckSelector("steps[assert-image]", s.Selector);
                    Check(s.ReferenceImage, "steps[assert-image].referenceImage");
                    if (s.MaxChannelDelta is < 0 or > 255)
                        diagnostics.Add(new("steps[assert-image].maxChannelDelta", "maxChannelDelta must be between 0 and 255."));
                    break;
            }
        }

        if (definition.Target.WindowTitleRegex is { } targetRegex)
            try { _ = new System.Text.RegularExpressions.Regex(targetRegex); }
            catch (ArgumentException ex) { diagnostics.Add(new("target.windowTitleRegex", $"Invalid regular expression: {ex.Message}")); }

        return diagnostics.ToArray();
    }

    private static void RejectUnknownKeys(YamlMappingNode map, string[] allowed, string path, List<WorkflowDiagnostic> diagnostics)
    {
        foreach (var child in map.Children)
        {
            var key = child.Key.ToString();
            if (!allowed.Contains(key, StringComparer.Ordinal)) diagnostics.Add(new(path, $"Unknown key '{key}'. Allowed keys: {string.Join(", ", allowed.OrderBy(k => k, StringComparer.Ordinal))}."));
        }
    }

    private static string? GetScalar(YamlMappingNode map, string key) =>
        map.Children.TryGetValue(new YamlScalarNode(key), out var node) ? node.ToString() : null;

    private static string? GetString(YamlMappingNode map, string key)
    {
        var value = GetScalar(map, key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? GetInt(YamlMappingNode map, string key) =>
        map.Children.TryGetValue(new YamlScalarNode(key), out var node) && int.TryParse(node.ToString(), out var value) ? value : null;

    private static bool? GetBool(YamlMappingNode map, string key) =>
        map.Children.TryGetValue(new YamlScalarNode(key), out var node) && bool.TryParse(node.ToString(), out var value) ? value : null;

    private static ProcessMatchMode ParseMatchMode(string? mode) =>
        mode is null ? ProcessMatchMode.Exact :
        mode.Equals("prefix", StringComparison.OrdinalIgnoreCase) ? ProcessMatchMode.Prefix :
        mode.Equals("exact", StringComparison.OrdinalIgnoreCase) ? ProcessMatchMode.Exact : ProcessMatchMode.Exact;

    private static ClickKind ParseClickKind(string? kind) => kind?.ToLowerInvariant() switch
    {
        "double" => ClickKind.Double,
        "right" => ClickKind.Right,
        _ => ClickKind.Single
    };

    private static FileCondition ParseFileCondition(string? condition) =>
        condition is not null && condition.Equals("not-exists", StringComparison.OrdinalIgnoreCase) ? FileCondition.NotExists : FileCondition.Exists;

    private static WindowCondition ParseWindowCondition(string? condition) => condition?.ToLowerInvariant() switch
    {
        "minimized" => WindowCondition.Minimized,
        "closed" => WindowCondition.Closed,
        "foreground" => WindowCondition.Foreground,
        _ => WindowCondition.Visible
    };

    private static ControlState ParseControlState(string? state) => state?.ToLowerInvariant() switch
    {
        "visible" => ControlState.Visible,
        "enabled" => ControlState.Enabled,
        "value" => ControlState.Value,
        _ => ControlState.Exists
    };

    private static bool IsValidPick(string pick) =>
        pick.Equals("first", StringComparison.OrdinalIgnoreCase) ||
        (pick.StartsWith("index:", StringComparison.Ordinal) && int.TryParse(pick["index:".Length..], out var n) && n >= 0) ||
        (int.TryParse(pick, out var m) && m >= 0);

    [System.Text.RegularExpressions.GeneratedRegex(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial System.Text.RegularExpressions.Regex Pattern();
}
