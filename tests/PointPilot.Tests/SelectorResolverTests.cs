using PointPilot.Core;
using PointPilot.Core.Elements;
using PointPilot.Core.Engine;
using PointPilot.Core.Selectors;

namespace PointPilot.Tests;

public sealed class SelectorResolverTests
{
    private static FakeElement El(string? id = null, string? name = null, string? className = null, string? type = null) =>
        new(new ElementIdentity(id, name, className, type));

    [Fact]
    public void UniqueAutomationId_ResolvesSingleElement()
    {
        var tree = new[] { El(name: "other"), El(id: "okButton", name: "OK", type: "button") };
        var matches = SelectorResolver.FindAll(tree, new SelectorCriteria("okButton", null, null, null));
        Assert.Single(matches);
    }

    [Fact]
    public void ZeroMatches_IsReportedAsZero()
    {
        var tree = new[] { El(id: "a"), El(id: "b") };
        var resolution = SelectorResolver.Resolve(tree, new SelectorCriteria("missing", null, null, null));
        Assert.IsType<SelectorResolution.ZeroMatches>(resolution);
    }

    [Fact]
    public void DuplicateAccessibleNames_AreAmbiguousWithoutPick()
    {
        // Two controls sharing the same accessible name must fail closed.
        var tree = new[] { El(id: "one", name: "Save", type: "button"), El(id: "two", name: "Save", type: "button") };
        var resolution = SelectorResolver.Resolve(tree, new SelectorCriteria(null, "save", null, "button"));
        var ambiguous = Assert.IsType<SelectorResolution.Ambiguous>(resolution);
        Assert.Equal(2, ambiguous.Matches.Count);
    }

    [Fact]
    public void DeclaredPick_SelectsFromMultipleMatches()
    {
        var first = El(id: "row-1", name: "Row");
        var second = El(id: "row-2", name: "Row");
        var (element, weak) = SelectorResolver.ApplyPick(
            new SelectorSpec(Pick: "index:1"),
            [first, second]);
        Assert.Same(second, element);
        Assert.True(weak);
    }

    [Fact]
    public void OutOfRangePick_Throws()
    {
        var tree = new[] { El(name: "Only") };
        Assert.Throws<StepFailureException>(() => SelectorResolver.ApplyPick(new SelectorSpec(Pick: "index:5"), tree));
    }

    [Fact]
    public void CriteriaMatch_CaseInsensitiveAndAllMustHold()
    {
        var tree = new[]
        {
            El(id: "goBtn", name: "Go", className: "ToolButton", type: "button"),
            El(id: "goBtn2", name: "Stop", className: "ToolButton", type: "button")
        };
        var matches = SelectorResolver.FindAll(tree, new SelectorCriteria("GOBTN", "gO", "toolbutton", "BUTTON"));
        Assert.Single(matches);
    }

    [Fact]
    public void WeakClassification_FlagsMissingStableIdentifiers()
    {
        Assert.False(WorkflowRunnerProxy.Weak(new SelectorSpec(AutomationId: "x")));
        Assert.False(WorkflowRunnerProxy.Weak(new SelectorSpec(Name: "x")));
        Assert.True(WorkflowRunnerProxy.Weak(new SelectorSpec(ClassName: "Canvas")));      // no id/name
        Assert.True(WorkflowRunnerProxy.Weak(new SelectorSpec(X: 5, Y: 6)));               // coordinates
        Assert.True(WorkflowRunnerProxy.Weak(new SelectorSpec(Name: "x", Pick: "first"))); // declared multiplicity
    }

    private static class WorkflowRunnerProxy
    {
        public static bool Weak(SelectorSpec spec) => WorkflowRunner.IsWeakSelector(spec);
    }
}
