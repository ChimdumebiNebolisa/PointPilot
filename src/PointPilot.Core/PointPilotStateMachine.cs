namespace PointPilot.Core;

public enum PointPilotState
{
    Idle, Connecting, Listening, Understanding, Teaching, Guiding,
    Planning, Acting, Verifying, Speaking, Paused, Error
}

public sealed class PointPilotStateMachine
{
    private readonly object _sync = new();
    private static readonly IReadOnlyDictionary<PointPilotState, PointPilotState[]> Allowed =
        new Dictionary<PointPilotState, PointPilotState[]>
        {
            [PointPilotState.Idle] = [PointPilotState.Connecting, PointPilotState.Error],
            [PointPilotState.Connecting] = [PointPilotState.Listening, PointPilotState.Paused, PointPilotState.Error],
            [PointPilotState.Listening] = [PointPilotState.Understanding, PointPilotState.Paused, PointPilotState.Idle, PointPilotState.Error],
            [PointPilotState.Understanding] = [PointPilotState.Teaching, PointPilotState.Guiding, PointPilotState.Planning, PointPilotState.Speaking, PointPilotState.Listening, PointPilotState.Paused, PointPilotState.Error],
            [PointPilotState.Teaching] = [PointPilotState.Speaking, PointPilotState.Listening, PointPilotState.Paused, PointPilotState.Error],
            [PointPilotState.Guiding] = [PointPilotState.Verifying, PointPilotState.Planning, PointPilotState.Speaking, PointPilotState.Listening, PointPilotState.Paused, PointPilotState.Error],
            [PointPilotState.Planning] = [PointPilotState.Acting, PointPilotState.Listening, PointPilotState.Paused, PointPilotState.Error],
            [PointPilotState.Acting] = [PointPilotState.Verifying, PointPilotState.Listening, PointPilotState.Paused, PointPilotState.Error],
            [PointPilotState.Verifying] = [PointPilotState.Acting, PointPilotState.Speaking, PointPilotState.Paused, PointPilotState.Listening, PointPilotState.Error],
            [PointPilotState.Speaking] = [PointPilotState.Listening, PointPilotState.Paused, PointPilotState.Error],
            [PointPilotState.Paused] = [PointPilotState.Listening, PointPilotState.Idle, PointPilotState.Error],
            [PointPilotState.Error] = [PointPilotState.Listening, PointPilotState.Idle]
        };

    public PointPilotState Current { get; private set; } = PointPilotState.Idle;
    public event EventHandler<PointPilotState>? Changed;
    public event EventHandler<InvalidStateTransition>? Rejected;

    public bool CanTransition(PointPilotState next)
    {
        lock (_sync)
        {
            return Current != next && Allowed[Current].Contains(next);
        }
    }

    public void Transition(PointPilotState next)
    {
        lock (_sync)
        {
            if (Current == next) return;
            if (!Allowed[Current].Contains(next))
            {
                Rejected?.Invoke(this, new InvalidStateTransition(Current, next));
                throw new InvalidOperationException($"Invalid PointPilot state transition: {Current} -> {next}.");
            }
            Current = next;
        }
        Changed?.Invoke(this, next);
    }
}

public sealed record InvalidStateTransition(PointPilotState From, PointPilotState To);
