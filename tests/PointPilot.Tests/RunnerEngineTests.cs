using PointPilot.Core;
using PointPilot.Core.Engine;
using PointPilot.Core.Elements;
using PointPilot.Core.Selectors;
using PointPilot.Core.Tracing;
using PointPilot.Core.Workflows;

namespace PointPilot.Tests;

/// <summary>
/// Adversarial engine behavior on fake Windows ports: every mandated failure mode
/// (ambiguity, disappearance, foreground loss, restart, cancellation, timeouts,
/// coordinate bounds, file assertions) fails closed with a recorded reason.
/// </summary>
public sealed class RunnerEngineTests
{
    private (WorkflowRunner Runner, FakeSession Session, FakeMonitor Monitor, FakeInputPort Input, FakeCapture Capture, ManualClock Clock) Build()
    {
        var session = new FakeSession();
        var monitor = new FakeMonitor();
        var input = new FakeInputPort();
        var capture = new FakeCapture();
        var clock = new ManualClock();
        var runner = new WorkflowRunner(new FakeSessionFactory(session), input, monitor, capture, new FakeImageComparer(), clock);
        return (runner, session, monitor, input, capture, clock);
    }

    [Fact]
    public async Task HappyPath_CompletesAndTracesEveryStep()
    {
        var (runner, session, _, input, _, _) = Build();
        session.Tree =
        [
            FakeElement.Make(id: "root", type: "window"),
            FakeElement.Make(id: "ok", name: "OK", type: "button")
        ];
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new FocusWindowStep(),
            new ClickStep(new SelectorSpec(AutomationId: "ok"), ClickKind.Single),
            new AssertFileStep(@"C:\definitely-missing-9f3.png", FileCondition.NotExists)
        ), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Completed", result.Trace.Status);
        Assert.Equal(3, result.Trace.Passed);
        Assert.Single(input.Clicks);
        Assert.Equal("engine-test", result.Trace.WorkflowName);
    }

    [Fact]
    public async Task AmbiguousSelector_FailsClosedListingMatchCount()
    {
        var (runner, session, _, input, _, _) = Build();
        session.Tree =
        [
            FakeElement.Make(name: "Save", type: "button"),
            FakeElement.Make(name: "Save", type: "button")
        ];
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new ClickStep(new SelectorSpec(Name: "Save"), ClickKind.Single)), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Failed", result.Trace.Status);
        Assert.Contains("2 elements", result.Trace.FailureReason, StringComparison.Ordinal);
        Assert.Empty(input.Clicks);
    }

    [Fact]
    public async Task ZeroMatchSelector_FailsWithSearchedCount()
    {
        var (runner, session, _, _, _, _) = Build();
        session.Tree = [FakeElement.Make(id: "a"), FakeElement.Make(id: "b")];
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new FocusControlStep(new SelectorSpec(AutomationId: "missing"))), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Failed", result.Trace.Status);
        Assert.Contains("matched no elements", result.Trace.FailureReason, StringComparison.Ordinal);
        Assert.Contains("2 elements", result.Trace.FailureReason, StringComparison.Ordinal); // root + 2
    }

    [Fact]
    public async Task ElementDisappearingBetweenResolveAndAction_FailsWithoutSendingInput()
    {
        var (runner, session, _, input, _, _) = Build();
        session.Tree = [FakeElement.Make(id: "ghost")];
        // The element resolves but the executor discovers it is gone at action time.
        input.ThrowOnAction = new StepFailureException("The resolved control disappeared before the click was sent.");
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new ClickStep(new SelectorSpec(AutomationId: "ghost"), ClickKind.Single)), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Failed", result.Trace.Status);
        Assert.Contains("disappeared", result.Trace.Steps[0].FailureReason, StringComparison.Ordinal);
        Assert.Empty(input.Clicks);
    }

    [Fact]
    public async Task ForegroundLoss_FailsBeforeInput()
    {
        var (runner, session, monitor, input, _, _) = Build();
        session.Tree = [FakeElement.Make(id: "btn")];
        monitor.Foreground = 0x9999; // another application stole focus
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new ClickStep(new SelectorSpec(AutomationId: "btn"), ClickKind.Single)), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Failed", result.Trace.Status);
        Assert.Contains("lost foreground", result.Trace.FailureReason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(input.Clicks);
    }

    [Fact]
    public async Task ProcessRestart_IsDetectedByPidChange()
    {
        var (runner, session, monitor, _, _, _) = Build();
        session.Tree = [FakeElement.Make(id: "btn")];
        monitor.ProcessIds[0x1234] = 777; // window reused by a restarted process with a new pid
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new PressStep(["CTRL", "S"])), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Failed", result.Trace.Status);
        Assert.Contains("restarted", result.Trace.FailureReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WindowTitleChange_IsObservedByWaitCondition()
    {
        var (runner, session, _, _, _, clock) = Build();
        session.Tree = [FakeElement.Make(id: "root")];
        session.Title = "Untitled";
        var polls = 0;
        clock.Advancing = _ => { if (++polls == 1) session.Title = "Saved — Notepad"; };
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new WaitStep(new WaitForWindowTitle("Saved.*"))), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Completed", result.Trace.Status);
        Assert.Contains("Title became 'Saved — Notepad'", result.Trace.Steps[0].ObservedPostcondition, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CoordinateOutsideWindowBounds_FailsClosed()
    {
        var (runner, session, _, _, _, _) = Build();
        session.Tree = [FakeElement.Make(id: "root")];
        session.LiveBounds = new WindowBounds(100, 100, 400, 300); // relative x=500 exceeds width 400
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new ClickStep(new SelectorSpec(X: 500, Y: 40), ClickKind.Single)), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Failed", result.Trace.Status);
        Assert.Contains("outside the target window bounds", result.Trace.FailureReason!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledElement_CannotBeClicked()
    {
        var (runner, session, _, input, _, _) = Build();
        session.Tree = [new FakeElement(new PointPilot.Core.Selectors.ElementIdentity("btn", null, null, null)) { IsEnabled = false }];
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new ClickStep(new SelectorSpec(AutomationId: "btn"), ClickKind.Single)), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Failed", result.Trace.Status);
        Assert.Contains("disabled", result.Trace.FailureReason!, StringComparison.Ordinal);
        Assert.Empty(input.Clicks);
    }

    [Fact]
    public async Task CancellationDuringMultiStepRun_MarksRunCancelledWithoutFailures()
    {
        var (runner, session, _, _, _, clock) = Build();
        session.Tree = [FakeElement.Make(id: "root")];
        using var cts = new CancellationTokenSource();
        clock.Advancing = _ => cts.Cancel(); // user presses Escape while the second step waits

        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new FocusWindowStep(),
            new WaitStep(new DelayMs(2000)),
            new FocusWindowStep("never reached")
        ), EngineTestHarness.Options(), cts.Token);

        Assert.Equal("Cancelled", result.Trace.Status);
        Assert.All(result.Trace.Steps, s => Assert.NotEqual(StepStatus.Failed, s.Status));
        Assert.True(result.Trace.Steps.Count < 3, "The final step must never start after cancellation.");
    }

    [Fact]
    public async Task WaitTimeout_FailsTheStepWithReason()
    {
        var (runner, session, _, _, _, _) = Build();
        session.Tree = [FakeElement.Make(id: "root")];
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new WaitStep(new WaitForWindowTitle("Never appears"), TimeoutMs: 400)), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Failed", result.Trace.Status);
        Assert.Contains("Timed out waiting", result.Trace.FailureReason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(FileCondition.Exists, true)]
    [InlineData(FileCondition.Exists, false)]
    [InlineData(FileCondition.NotExists, true)]
    [InlineData(FileCondition.NotExists, false)]
    public async Task FileAssertions_PassAndFailBothDirections(FileCondition condition, bool filePresent)
    {
        var path = Path.Combine(Path.GetTempPath(), $"pointpilot-test-{Guid.NewGuid():N}.txt");
        if (filePresent) await File.WriteAllTextAsync(path, "data");
        try
        {
            var (runner, session, _, _, _, _) = Build();
            session.Tree = [FakeElement.Make(id: "root")];
            var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
                new AssertFileStep(path, condition)), EngineTestHarness.Options(), CancellationToken.None);

            var shouldPass = (condition == FileCondition.Exists) == filePresent;
            Assert.Equal(shouldPass ? "Completed" : "Failed", result.Trace.Status);
            if (!shouldPass) Assert.Contains("File assertion failed", result.Trace.FailureReason, StringComparison.Ordinal);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Theory]
    [InlineData(1.0, 8, "Completed")]
    [InlineData(0.995, 8, "Failed")]
    public async Task ImageAssertion_ThresholdDecides(double fraction, int delta, string expected)
    {
        var session = new FakeSession();
        session.Tree = [FakeElement.Make(id: "canvas", className: "Canvas")];
        var comparer = new FakeImageComparer { Fraction = fraction };
        var reference = Path.Combine(Path.GetTempPath(), $"ref-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(reference, [1]);
        try
        {
            var runner = new WorkflowRunner(new FakeSessionFactory(session), new FakeInputPort(), new FakeMonitor(), new FakeCapture(), comparer, new ManualClock());
            var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
                new AssertImageStep(new SelectorSpec(AutomationId: "canvas"), reference, delta)), EngineTestHarness.Options(), CancellationToken.None);
            Assert.Equal(expected, result.Trace.Status);
        }
        finally { if (File.Exists(reference)) File.Delete(reference); }
    }

    [Fact]
    public async Task DryRun_ResolvesSelectorsButSendsNoInput()
    {
        var (runner, session, _, input, capture, _) = Build();
        session.Tree = [FakeElement.Make(id: "btn", name: "OK", type: "button")];
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new FocusWindowStep(),
            new ClickStep(new SelectorSpec(AutomationId: "btn"), ClickKind.Single),
            new PressStep(["ENTER"]),
            new ScreenshotStep()
        ), EngineTestHarness.Options(dryRun: true), CancellationToken.None);

        Assert.Equal("Completed", result.Trace.Status);
        Assert.Empty(input.Clicks);
        Assert.Empty(input.Pressed);
        Assert.Equal(0, capture.Calls);
        Assert.Contains("(dry-run)", result.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TraceArtifacts_ArePersistedToOutputDirectory()
    {
        var (runner, session, _, _, _, _) = Build();
        session.Tree = [FakeElement.Make(id: "root")];
        var output = Path.Combine(Path.GetTempPath(), $"pointpilot-trace-{Guid.NewGuid():N}");
        try
        {
            var result = await runner.ExecuteAsync(EngineTestHarness.Definition(new FocusWindowStep()),
                EngineTestHarness.Options(output: output), CancellationToken.None);
            Assert.True(File.Exists(Path.Combine(output, "trace.json")));
            Assert.True(File.Exists(Path.Combine(output, "summary.txt")));
            Assert.Contains(result.Trace.RunId.ToString(), await File.ReadAllTextAsync(Path.Combine(output, "trace.json")));
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, recursive: true); }
    }

    [Fact]
    public async Task WeakTargets_AreFlaggedInTrace()
    {
        var (runner, session, _, _, _, _) = Build();
        session.Tree = [FakeElement.Make(className: "CanvasOnly")];
        var result = await runner.ExecuteAsync(EngineTestHarness.Definition(
            new ClickStep(new SelectorSpec(ClassName: "CanvasOnly"), ClickKind.Single)), EngineTestHarness.Options(), CancellationToken.None);

        Assert.Equal("Completed", result.Trace.Status);
        Assert.True(result.Trace.Steps[0].Resolved!.WeakTarget);
    }
}
