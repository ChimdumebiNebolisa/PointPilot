using System.Text.RegularExpressions;
using PointPilot.Core.Selectors;

namespace PointPilot.Core.Workflows;

/// <summary>
/// Substitutes ${name} placeholders in workflow string fields. Unknown references are
/// reported as diagnostics before any run begins; values are substituted verbatim with
/// no expression evaluation so behavior stays deterministic.
/// </summary>
public static partial class VariableSubstituter
{
    [GeneratedRegex(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}")]
    public static partial Regex Pattern();

    public static (WorkflowDefinition Resolved, IReadOnlyList<WorkflowDiagnostic> Diagnostics) Resolve(
        WorkflowDefinition definition, IReadOnlyDictionary<string, string> provided)
    {
        var diagnostics = new List<WorkflowDiagnostic>();
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variable in definition.Variables)
        {
            if (provided.TryGetValue(variable.Name, out var value))
            {
                values[variable.Name] = value;
                continue;
            }
            if (variable.Default is not null) { values[variable.Name] = variable.Default; continue; }
            diagnostics.Add(new("variables", $"Missing variable '{variable.Name}'. Provide it via --var {variable.Name}=... or declare a default."));
        }

        if (diagnostics.Count > 0) return (definition, diagnostics);

        var resolvedTarget = new TargetSpec(
            Substitute(definition.Target.ProcessName, values),
            definition.Target.ProcessNameMatch,
            definition.Target.WindowTitleRegex is null ? null : Substitute(definition.Target.WindowTitleRegex, values));

        var resolvedSteps = definition.Steps.Select(step => SubstituteStep(step, values)).ToList();
        var resolved = definition with { Target = resolvedTarget, Steps = resolvedSteps };
        return (resolved, []);
    }

    private static StepSpec SubstituteStep(StepSpec step, IReadOnlyDictionary<string, string> values) => step switch
    {
        ClickStep s => s with { Selector = SubstituteSelector(s.Selector, values) },
        FocusControlStep s => s with { Selector = SubstituteSelector(s.Selector, values) },
        TypeTextStep s => s with { Text = Substitute(s.Text, values), Selector = s.Selector is null ? null : SubstituteSelector(s.Selector, values) },
        PressStep s => s with { Keys = [.. s.Keys.Select(k => Substitute(k, values))] },
        WaitStep s => s with { Condition = SubstituteCondition(s.Condition, values) },
        AssertFileStep s => s with { Path = Substitute(s.Path, values) },
        AssertControlStep s => s with { Value = s.Value is null ? null : Substitute(s.Value, values), Selector = SubstituteSelector(s.Selector, values) },
        AssertImageStep s => s with { ReferenceImage = Substitute(s.ReferenceImage, values), Selector = SubstituteSelector(s.Selector, values) },
        _ => step
    };

    private static WaitCondition SubstituteCondition(WaitCondition condition, IReadOnlyDictionary<string, string> values) => condition switch
    {
        WaitForWindowTitle w => new WaitForWindowTitle(Substitute(w.Regex, values)),
        WaitForControl w => new WaitForControl(SubstituteSelector(w.Selector, values), w.State),
        WaitForFile w => new WaitForFile(Substitute(w.Path, values), w.Condition),
        _ => condition
    };

    private static SelectorSpec SubstituteSelector(SelectorSpec selector, IReadOnlyDictionary<string, string> values) => selector with
    {
        AutomationId = selector.AutomationId is null ? null : Substitute(selector.AutomationId, values),
        Name = selector.Name is null ? null : Substitute(selector.Name, values),
        ClassName = selector.ClassName is null ? null : Substitute(selector.ClassName, values),
        Role = selector.Role is null ? null : Substitute(selector.Role, values)
    };

    public static string Substitute(string value, IReadOnlyDictionary<string, string> values) =>
        Pattern().Replace(value, match => values.TryGetValue(match.Groups[1].Value, out var replacement) ? replacement : match.Value);
}
