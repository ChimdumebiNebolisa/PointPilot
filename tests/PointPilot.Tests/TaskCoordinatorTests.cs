using PointPilot.Core;

namespace PointPilot.Tests;

public sealed class TaskCoordinatorTests
{
    [Fact]
    public void Interrupt_InvalidatesOldLeaseAndPreservesCompletedSteps()
    {
        using var coordinator = new TaskCoordinator();
        coordinator.Start("Improve graphic");
        var oldLease = coordinator.GetLease();
        coordinator.RecordCompleted(oldLease, "Updated title");

        var revised = coordinator.Interrupt("Keep the subtitle");

        Assert.False(coordinator.IsCurrent(oldLease));
        Assert.True(oldLease.CancellationToken.IsCancellationRequested);
        Assert.Single(revised.CompletedActions);
        Assert.Contains("Keep the subtitle", revised.Constraints);
        Assert.True(coordinator.IsCurrent(coordinator.GetLease()));
    }

    [Fact]
    public void Confirmation_IsBoundToExactTaskRevisionActionAndPath()
    {
        using var coordinator = new TaskCoordinator();
        var path = Path.GetFullPath("promo.png");
        coordinator.Start("Export", requiresConfirmation: true, confirmationAction: "Export PNG", targetPath: path);

        Assert.Throws<InvalidOperationException>(() => coordinator.GetLease());
        Assert.Throws<InvalidOperationException>(() => coordinator.Confirm("Export PNG", Path.GetFullPath("other.png")));
        coordinator.Confirm("Export PNG", path);
        var confirmedLease = coordinator.GetLease();
        coordinator.Interrupt("Change subtitle first");

        Assert.False(coordinator.IsCurrent(confirmedLease));
        Assert.Throws<InvalidOperationException>(() => coordinator.GetLease());
    }

    [Fact]
    public void Revise_PreservesCompletedStepsAndOriginalGoalConstraint()
    {
        using var coordinator = new TaskCoordinator();
        coordinator.Start("Create a polished graphic");
        var lease = coordinator.GetLease();
        coordinator.RecordCompleted(lease, "Added shadow");

        var revised = coordinator.Revise("Make the shadow more subtle");

        Assert.Single(revised.CompletedActions);
        Assert.Contains(revised.Constraints, value => value.Contains("Original goal", StringComparison.Ordinal));
        Assert.Equal("Make the shadow more subtle", revised.Goal);
    }

    [Fact]
    public void Pause_ImmediatelyCancelsAndInvalidatesTheCurrentLease()
    {
        using var coordinator = new TaskCoordinator();
        coordinator.Start("Apply a shadow");
        var lease = coordinator.GetLease();
        coordinator.Pause();
        Assert.True(lease.CancellationToken.IsCancellationRequested);
        Assert.False(coordinator.IsCurrent(lease));
    }
}
