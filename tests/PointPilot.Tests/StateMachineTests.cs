using PointPilot.Core;

namespace PointPilot.Tests;

public sealed class StateMachineTests
{
    [Fact]
    public void HappyPath_CoversListeningActingVerificationAndSpeech()
    {
        var state = new PointPilotStateMachine();
        foreach (var next in new[] { PointPilotState.Connecting, PointPilotState.Listening, PointPilotState.Understanding, PointPilotState.Planning, PointPilotState.Acting, PointPilotState.Verifying, PointPilotState.Speaking, PointPilotState.Listening })
            state.Transition(next);
        Assert.Equal(PointPilotState.Listening, state.Current);
    }

    [Fact]
    public void InvalidTransition_IsRejectedWithoutChangingState()
    {
        var state = new PointPilotStateMachine();
        Assert.Throws<InvalidOperationException>(() => state.Transition(PointPilotState.Acting));
        Assert.Equal(PointPilotState.Idle, state.Current);
    }
}
