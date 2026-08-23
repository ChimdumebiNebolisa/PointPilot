using PointPilot.Core.Engine;
using PointPilot.Core.Recording;
using PointPilot.Core.Selectors;
using PointPilot.Core.Workflows;

namespace PointPilot.Tests;

public sealed class RecorderBuilderTests
{
    [Fact]
    public void InvokedControls_BecomeClickStepsWithStableSelector()
    {
        var builder = new RecorderSessionBuilder();
        builder.Add(new RecorderFocusChanged(new RecorderControlInfo("mainEdit", "Text", "TextBox", "edit")));
        builder.Add(new RecorderInvoked(new RecorderControlInfo("saveBtn", "Save", "Button", "button")));
        builder.Add(new RecorderFocusChanged(null));
        var steps = builder.Finish();

        Assert.Single(steps);
        var click = Assert.IsType<ClickStep>(steps[0]);
        Assert.Equal("saveBtn", click.Selector.AutomationId);
        Assert.Equal(ClickKind.Single, click.Kind);
    }

    [Fact]
    public void TypedCharacters_AccumulateAndFlushIntoTypeTextSteps()
    {
        var builder = new RecorderSessionBuilder();
        builder.Add(new RecorderKeyDown("h", Ctrl: false, Alt: false, Win: false, 'h'));
        builder.Add(new RecorderKeyDown("i", Ctrl: false, Alt: false, Win: false, 'i'));
        builder.Add(new RecorderKeyDown("ENTER", Ctrl: false, Alt: false, Win: false, '\r')); // enter flushes
        var steps = builder.Finish();

        var type = Assert.IsType<TypeTextStep>(steps[0]);
        Assert.Equal("hi", type.Text);
    }

    [Fact]
    public void ModifierKeys_ProducePressSteps()
    {
        var builder = new RecorderSessionBuilder();
        builder.Add(new RecorderKeyDown("S", Ctrl: true, Alt: false, Win: false, 's'));
        var steps = builder.Finish();
        var press = Assert.IsType<PressStep>(steps[0]);
        Assert.Equal(["CTRL", "S"], press.Keys);
    }

    [Fact]
    public void FocusChange_FlushesPendingTextAgainstTheFocusedControl()
    {
        var builder = new RecorderSessionBuilder();
        builder.Add(new RecorderFocusChanged(new RecorderControlInfo("titleBox", "Title", null, "edit")));
        builder.Add(new RecorderKeyDown("x", Ctrl: false, Alt: false, Win: false, 'x'));
        builder.Add(new RecorderFocusChanged(null)); // leaving the control flushes
        var steps = builder.Finish();

        var type = Assert.IsType<TypeTextStep>(steps[0]);
        Assert.Equal("titleBox", type.Selector!.AutomationId);
    }

    [Fact]
    public void WeakSelectors_AreProducedWithoutAutomationIdOrName_AndFlagged()
    {
        var info = new RecorderControlInfo(null, null, "CustomCanvas", "custom");
        Assert.True(RecorderSessionBuilder.IsWeak(info));
        var selector = RecorderSessionBuilder.BuildSelector(info);
        Assert.Equal("CustomCanvas", selector.ClassName);
        Assert.True(WorkflowRunner.IsWeakSelector(selector));

        var strong = new RecorderControlInfo("ok", "OK", "Button", "button");
        Assert.False(RecorderSessionBuilder.IsWeak(strong));
        Assert.False(WorkflowRunner.IsWeakSelector(RecorderSessionBuilder.BuildSelector(strong)));
    }

    [Fact]
    public void RecordedDraft_NeverContainsCoordinateSelectors()
    {
        // A recorder emitting coordinates would silently create brittle replays.
        var builder = new RecorderSessionBuilder();
        builder.Add(new RecorderInvoked(new RecorderControlInfo(null, null, null, "custom")));
        builder.Add(new RecorderInvoked(new RecorderControlInfo("id", null, null, "button")));
        foreach (var step in builder.Finish())
        {
            if (step is ClickStep { Selector: { } selector })
                Assert.False(selector.IsCoordinate);
            else
                throw new InvalidOperationException($"Unexpected step kind {step.GetType().Name}");
        }
    }
}
