using System.Text.RegularExpressions;

using PointPilot.Core.Elements;

namespace PointPilot.Core.Selectors;

public sealed record SelectorSpec(
    string? AutomationId = null,
    string? Name = null,
    string? ClassName = null,
    string? Role = null,
    string? Pick = null,
    int? X = null,
    int? Y = null)
{
    public bool IsCoordinate => X is not null || Y is not null;
}

/// <summary>
/// Normalized, validated selector criteria used for subtree matching.
/// Coordinate selectors bypass this model entirely and are handled by the runner.
/// </summary>
public sealed record SelectorCriteria(string? AutomationId, string? Name, string? ClassName, string? Role)
{
    public override string ToString()
    {
        var parts = new[]
        {
            AutomationId is null ? null : $"automationId={AutomationId}",
            Name is null ? null : $"name={Name}",
            ClassName is null ? null : $"className={ClassName}",
            Role is null ? null : $"role={Role}"
        }.Where(p => p is not null);
        return "{" + string.Join(", ", parts) + "}";
    }
}

public sealed record ElementIdentity(string? AutomationId, string? Name, string? ClassName, string? ControlType);

public abstract record SelectorResolution
{
    public sealed record Unique(IUiElement Element, int Examined) : SelectorResolution;

    public sealed record Ambiguous(IReadOnlyList<ElementIdentity> Matches) : SelectorResolution;

    public sealed record ZeroMatches : SelectorResolution;
}

public static class SelectorResolver
{
    /// <summary>
    /// Collects every element of the subtree matching the criteria. Matching is exact
    /// and case-insensitive per property; all provided criteria must match simultaneously.
    /// </summary>
    public static IReadOnlyList<IUiElement> FindAll(IEnumerable<IUiElement> subtree, SelectorCriteria criteria)
    {
        var matches = new List<IUiElement>();
        foreach (var element in subtree)
        {
            var id = element.Identity;
            if (criteria.AutomationId is not null && !Matches(id.AutomationId, criteria.AutomationId)) continue;
            if (criteria.Name is not null && !Matches(id.Name, criteria.Name)) continue;
            if (criteria.ClassName is not null && !Matches(id.ClassName, criteria.ClassName)) continue;
            if (criteria.Role is not null && !Matches(id.ControlType, criteria.Role)) continue;
            matches.Add(element);
        }
        return matches;
    }

    /// <summary>
    /// Applies the declared pick policy. A missing pick requires a unique match; any
    /// declared pick tolerates multiplicity but the resolution is recorded as weak.
    /// </summary>
    public static (IUiElement Element, bool WeakTarget) ApplyPick(SelectorSpec spec, IReadOnlyList<IUiElement> matches)
    {
        if (spec.Pick is null)
            return (matches[0], false);
        var index = spec.Pick.Equals("first", StringComparison.OrdinalIgnoreCase) ? 0 : int.Parse(spec.Pick.StartsWith("index:", StringComparison.Ordinal) ? spec.Pick["index:".Length..] : spec.Pick, System.Globalization.CultureInfo.InvariantCulture);
        if (index < 0 || index >= matches.Count)
            throw new StepFailureException($"Selector {Describe(spec)} matched {matches.Count} element(s); declared pick index {index} is out of range.");
        return (matches[index], true);
    }

    public static SelectorResolution Resolve(IEnumerable<IUiElement> subtree, SelectorCriteria criteria)
    {
        var matches = FindAll(subtree, criteria);
        return matches.Count switch
        {
            0 => new SelectorResolution.ZeroMatches(),
            1 => new SelectorResolution.Unique(matches[0], matches.Count),
            _ => new SelectorResolution.Ambiguous([.. matches.Select(m => m.Identity)])
        };
    }

    public static string Describe(SelectorSpec spec)
    {
        if (spec.IsCoordinate) return $"{{ x={spec.X}, y={spec.Y} }}";
        var criteria = new SelectorCriteria(spec.AutomationId, spec.Name, spec.ClassName, spec.Role);
        return spec.Pick is null ? criteria.ToString() : $"{criteria} pick={spec.Pick}";
    }

    private static bool Matches(string? actual, string expected) =>
        actual is not null && string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
}
