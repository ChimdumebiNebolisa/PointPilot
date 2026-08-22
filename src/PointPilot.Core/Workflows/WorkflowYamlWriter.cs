using System.Text;
using PointPilot.Core.Selectors;

namespace PointPilot.Core.Workflows;

/// <summary>Deterministic YAML emission for workflow definitions (recorder drafts and saved edits).</summary>
public static class WorkflowYamlWriter
{
    public static string Write(WorkflowDefinition definition)
    {
        var sb = new StringBuilder();
        sb.AppendLine("schemaVersion: 1");
        sb.AppendLine($"name: {Quote(definition.Name)}");
        if (!string.IsNullOrWhiteSpace(definition.Description))
            sb.AppendLine($"description: {Quote(definition.Description)}");

        if (definition.Variables.Count > 0)
        {
            sb.AppendLine("variables:");
            foreach (var variable in definition.Variables)
            {
                if (variable.Default is null && variable.Required)
                    sb.AppendLine($"  {Quote(variable.Name)}:");
                else if (variable.Default is null)
                    sb.AppendLine($"  {Quote(variable.Name)}: {{ required: true }}");
                else
                    sb.AppendLine($"  {Quote(variable.Name)}: {Quote(variable.Default)}");
            }
        }

        sb.AppendLine("defaults:");
        sb.AppendLine($"  timeoutMs: {definition.Defaults.TimeoutMs}");
        sb.AppendLine("target:");
        sb.AppendLine($"  processName: {Quote(definition.Target.ProcessName)}");
        if (definition.Target.ProcessNameMatch == ProcessMatchMode.Prefix)
            sb.AppendLine("  processNameMatch: prefix");
        if (definition.Target.WindowTitleRegex is { } regex)
            sb.AppendLine($"  windowTitleRegex: {Quote(regex)}");

        sb.AppendLine("steps:");
        foreach (var step in definition.Steps) WriteStep(sb, step);
        return sb.ToString();
    }

    private static void WriteStep(StringBuilder sb, StepSpec step)
    {
        var fields = new List<string>();
        var kind = step switch
        {
            FocusWindowStep => "focus-window",
            ClickStep c => c.Kind switch
            {
                ClickKind.Double => "double-click",
                ClickKind.Right => "right-click",
                _ => "click"
            },
            TypeTextStep => "type-text",
            PressStep => "press",
            FocusControlStep => "focus-control",
            WaitStep => "wait",
            ScreenshotStep => "screenshot",
            AssertFileStep => "assert-file",
            AssertWindowStep => "assert-window",
            AssertControlStep => "assert-control",
            AssertImageStep => "assert-image",
            _ => step.GetType().Name
        };
        fields.Add($"step: {kind}");
        if (step.Name is { } name) fields.Add($"name: {Quote(name)}");
        if (step.TimeoutMs is { } timeout) fields.Add($"timeoutMs: {timeout}");

        switch (step)
        {
            case ClickStep click:
                fields.Add(WriteSelector(click.Selector));
                break;
            case TypeTextStep type:
                fields.Add($"text: {Quote(type.Text)}");
                if (type.Selector is not null) fields.Add(WriteSelector(type.Selector));
                break;
            case PressStep press:
                fields.Add($"keys: [{string.Join(", ", press.Keys.Select(Quote))}]");
                break;
            case FocusControlStep focus:
                fields.Add(WriteSelector(focus.Selector));
                break;
            case WaitStep wait:
                fields.Add("until:");
                fields.AddRange(WriteCondition(wait.Condition).Select(line => "  " + line));
                break;
            case AssertFileStep file:
                fields.Add($"path: {Quote(file.Path)}");
                if (file.Condition == FileCondition.NotExists) fields.Add("condition: not-exists");
                break;
            case AssertWindowStep window:
                fields.Add($"condition: {window.Condition.ToString().ToLowerInvariant()}");
                break;
            case AssertControlStep control:
                fields.Add(WriteSelector(control.Selector));
                fields.Add($"state: {control.State.ToString().ToLowerInvariant()}");
                if (control.Value is { } value) fields.Add($"value: {Quote(value)}");
                break;
            case AssertImageStep image:
                fields.Add(WriteSelector(image.Selector));
                fields.Add($"referenceImage: {Quote(image.ReferenceImage)}");
                fields.Add($"maxChannelDelta: {image.MaxChannelDelta}");
                break;
        }

        for (var i = 0; i < fields.Count; i++)
        {
            var line = fields[i];
            if (i == 0 || line.StartsWith(" ") || line.EndsWith(":") || line.EndsWith(": "))
                sb.AppendLine("- " + line);
            else
                sb.AppendLine("  " + line);
        }
    }

    private static IEnumerable<string> WriteCondition(WaitCondition condition) => condition switch
    {
        WaitForWindowTitle w => [$"windowTitleRegex: {Quote(w.Regex)}"],
        WaitForControl c => [$"control:", $"  {WriteSelector(c.Selector)}", $"state: {c.State.ToString().ToLowerInvariant()}"],
        WaitForFile f => [$"file: {Quote(f.Path)}", .. (f.Condition == FileCondition.NotExists ? new[] { "fileCondition: not-exists" } : Array.Empty<string>())],
        DelayMs d => [$"delayMs: {d.Milliseconds}"],
        _ => []
    };

    internal static string WriteSelector(SelectorSpec selector)
    {
        if (selector.IsCoordinate)
            return $"selector: {{ x: {selector.X}, y: {selector.Y} }}";
        var parts = new List<string>();
        if (selector.AutomationId is { } id) parts.Add($"automationId: {Quote(id)}");
        if (selector.Name is { } name) parts.Add($"name: {Quote(name)}");
        if (selector.ClassName is { } className) parts.Add($"className: {Quote(className)}");
        if (selector.Role is { } role) parts.Add($"role: {Quote(role)}");
        if (selector.Pick is { } pick) parts.Add($"pick: {Quote(pick)}");
        return $"selector: {{ {string.Join(", ", parts)} }}";
    }

    private static string Quote(string value)
    {
        var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        return $"\"{escaped}\"";
    }
}
