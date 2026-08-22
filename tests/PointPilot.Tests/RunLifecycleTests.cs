using PointPilot.Core.Engine;

namespace PointPilot.Tests;

public sealed class RunLifecycleTests
{
    [Fact]
    public void HappyPath_TransitionsThroughTheLockedTable()
    {
        var machine = new RunStateMachine();
        foreach (var state in new[] { RunState.Validating, RunState.Binding, RunState.Running, RunState.Completed })
            machine.Transition(state);
        Assert.Equal(RunState.Completed, machine.Current);
    }

    [Fact]
    public void IllegalJump_IsRejectedAndStateUnchanged()
    {
        var machine = new RunStateMachine();
        (RunState From, RunState To)? rejected = null;
        machine.Rejected += (_, t) => rejected = t;
        Assert.Throws<InvalidOperationException>(() => machine.Transition(RunState.Completed));
        Assert.Equal(RunState.Idle, machine.Current);
        Assert.Equal((RunState.Idle, RunState.Completed), rejected);
    }

    [Fact]
    public void FailedRun_CanOnlyReturnToIdle()
    {
        var machine = new RunStateMachine();
        machine.Transition(RunState.Validating);
        machine.Transition(RunState.Failed);
        Assert.Throws<InvalidOperationException>(() => machine.Transition(RunState.Running));
        machine.Transition(RunState.Idle);
        Assert.Equal(RunState.Idle, machine.Current);
    }

    [Fact]
    public void ControllerLease_IsCurrentUntilStopped()
    {
        using var controller = new RunController();
        var lease = controller.Lease;
        Assert.True(controller.IsCurrent(lease));
        controller.Stop();
        Assert.False(controller.IsCurrent(lease));
    }

    [Fact]
    public void Stop_IsIdempotentAndCancelsOutstandingLeaseTokens()
    {
        using var controller = new RunController();
        var lease = controller.Lease;
        controller.Stop();
        controller.Stop();
        Assert.True(lease.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    public void LeaseFromAnotherRun_IsNeverCurrent()
    {
        using var first = new RunController();
        using var second = new RunController();
        Assert.False(second.IsCurrent(first.Lease));
    }
}
