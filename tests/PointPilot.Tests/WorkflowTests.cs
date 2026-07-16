using PointPilot.Core;

namespace PointPilot.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public async Task Teach_CapturesGroundsAndPointsWithoutComputerUse()
    {
        var harness = new WorkflowHarness();
        var outcome = await harness.Workflow.TeachAsync("What does Layers do?", CancellationToken.None);

        Assert.Equal("Layers organize the composition.", outcome.Summary);
        Assert.Equal(new WindowBounds(10, 20, 30, 40), outcome.Target);
        Assert.Equal(0, harness.Computer.Calls);
        Assert.Equal(PointPilotState.Speaking, harness.State.Current);
    }

    [Fact]
    public async Task Guide_ProducesOneGroundedStep()
    {
        var harness = new WorkflowHarness();
        var outcome = await harness.Workflow.GuideAsync("Add a shadow", "", CancellationToken.None);

        Assert.Contains("Layers", outcome.Summary, StringComparison.Ordinal);
        Assert.Equal(1, harness.Visual.Calls);
        Assert.Equal(0, harness.Computer.Calls);
    }

    [Fact]
    public async Task GuideToAct_PreservesTheGuidedGoal()
    {
        var harness = new WorkflowHarness();
        await harness.Workflow.GuideAsync("Make the product stand out", "A subtle shadow is visible", CancellationToken.None);
        harness.State.Transition(PointPilotState.Listening);
        await harness.Workflow.ActAsync("Actually, do it for me", ["Keep the headline"], null, CancellationToken.None);
        Assert.Equal("Make the product stand out", harness.Computer.LastGoal);
        Assert.Contains(harness.Computer.LastConstraints, value => value == "Keep the headline");
    }

    [Fact]
    public async Task Act_RunsComputerUseThenRequiresVerificationBeforeSuccess()
    {
        var harness = new WorkflowHarness();
        var outcome = await harness.Workflow.ActAsync("Make the shadow subtle", ["Keep the title"], null, CancellationToken.None);

        Assert.True(outcome.Verification?.Succeeded);
        Assert.StartsWith("Done and verified", outcome.Summary, StringComparison.Ordinal);
        Assert.Equal(1, harness.Computer.Calls);
        Assert.Equal(1, harness.Verification.Calls);
        Assert.Single(harness.Tasks.Snapshot.CompletedActions);
    }

    [Fact]
    public async Task ConsequentialAct_WaitsForExactConfirmation()
    {
        var harness = new WorkflowHarness();
        var path = Path.GetFullPath("pointpilot-promo.png");
        var pending = await harness.Workflow.ActAsync("Export the graphic as PNG", [], path, CancellationToken.None);

        Assert.True(pending.RequiresConfirmation);
        Assert.Equal(0, harness.Computer.Calls);
        var completed = await harness.Workflow.ConfirmAndExecuteAsync(pending.ConfirmationAction!, path, CancellationToken.None);
        Assert.True(completed.Verification?.Succeeded);
        Assert.Equal(1, harness.Computer.Calls);
    }

    [Fact]
    public async Task FailedVerification_NeverClaimsDone()
    {
        var harness = new WorkflowHarness(verificationSucceeded: false);
        var outcome = await harness.Workflow.ActAsync("Make the shadow subtle", [], null, CancellationToken.None);

        Assert.False(outcome.Verification?.Succeeded);
        Assert.StartsWith("I stopped without claiming completion", outcome.Summary, StringComparison.Ordinal);
        Assert.Empty(harness.Tasks.Snapshot.CompletedActions);
    }

    [Fact]
    public async Task ConsequentialAct_RejectsNonPngPathBeforeComputerUse()
    {
        var harness = new WorkflowHarness();
        var outcome = await harness.Workflow.ActAsync("Export the graphic", [], Path.GetFullPath("graphic.jpg"), CancellationToken.None);
        Assert.False(outcome.RequiresConfirmation);
        Assert.Contains("only", outcome.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, harness.Computer.Calls);
    }
}

internal sealed class WorkflowHarness
{
    internal PointPilotStateMachine State { get; } = new();
    internal TaskCoordinator Tasks { get; } = new();
    internal FakeComputer Computer { get; } = new();
    internal FakeVisual Visual { get; } = new();
    internal FakeVerification Verification { get; }
    internal PointPilotWorkflow Workflow { get; }

    internal WorkflowHarness(bool verificationSucceeded = true)
    {
        State.Transition(PointPilotState.Connecting);
        State.Transition(PointPilotState.Listening);
        Verification = new FakeVerification(verificationSucceeded);
        Workflow = new PointPilotWorkflow(State, Tasks, new FakeWindows(), Visual, Computer, Verification);
    }
}

internal sealed class FakeWindows : IWindowContextService
{
    private int _capture;
    public Task<WindowSnapshot> CaptureForegroundAsync(CancellationToken cancellationToken) => Task.FromResult(Fixture.Snapshot(png: [(byte)++_capture]));
}

internal sealed class FakeVisual : IVisualReasoningService
{
    public int Calls { get; private set; }
    public Task<VisualGroundingResult> AnalyzeAsync(string request, WindowSnapshot snapshot, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(new VisualGroundingResult("Layers organize the composition.", new WindowBounds(10, 20, 30, 40), true));
    }
}

internal sealed class FakeComputer : IComputerUseService
{
    public int Calls { get; private set; }
    public string? LastGoal { get; private set; }
    public IReadOnlyList<string> LastConstraints { get; private set; } = [];
    public Task<ComputerRunResult> RunAsync(TaskLease lease, string goal, IReadOnlyList<string> constraints, CancellationToken cancellationToken)
    {
        Calls++;
        LastGoal = goal;
        LastConstraints = constraints;
        return Task.FromResult(new ComputerRunResult(true, "Computer actions completed.", 3));
    }
}

internal sealed class FakeVerification(bool succeeded) : IVerificationService
{
    public int Calls { get; private set; }
    public Task<VerificationResult> VerifyAsync(string goal, WindowSnapshot before, WindowSnapshot after, string? expectedFilePath, FileCheckpoint? beforeFile, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(succeeded ? new VerificationResult(true, true, "The expected result is visible.") : VerificationResult.Uncertain("The result is ambiguous."));
    }
}

internal static class Fixture
{
    internal static WindowSnapshot Snapshot(nint handle = 42, string process = "gimp-3.0", WindowBounds? bounds = null, byte[]? png = null) =>
        new(handle, process, "PointPilot demo — GIMP", bounds ?? new WindowBounds(0, 0, 800, 600), 800, 600, png ?? [1], new ScreenPoint(4, 5));
}
