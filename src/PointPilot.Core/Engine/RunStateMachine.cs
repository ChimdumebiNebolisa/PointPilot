namespace PointPilot.Core.Engine;

public enum RunState { Idle, Validating, Binding, Running, Completed, Failed, Cancelled }

/// <summary>Locked transition table for run lifecycle states; illegal jumps are rejected.</summary>
public sealed class RunStateMachine
{
    private readonly object _sync = new();
    private static readonly IReadOnlyDictionary<RunState, RunState[]> Allowed = new Dictionary<RunState, RunState[]>
    {
        [RunState.Idle] = [RunState.Validating],
        [RunState.Validating] = [RunState.Binding, RunState.Failed],
        [RunState.Binding] = [RunState.Running, RunState.Failed, RunState.Cancelled],
        [RunState.Running] = [RunState.Completed, RunState.Failed, RunState.Cancelled],
        [RunState.Completed] = [],
        [RunState.Failed] = [RunState.Idle],
        [RunState.Cancelled] = [RunState.Idle]
    };

    public RunState Current { get { lock (_sync) return _current; } }
    private RunState _current = RunState.Idle;

    public event EventHandler<RunState>? Changed;
    public event EventHandler<(RunState From, RunState To)>? Rejected;

    public bool CanTransition(RunState next)
    {
        lock (_sync) return _current != next && Allowed[_current].Contains(next);
    }

    public void Transition(RunState next)
    {
        RunState before;
        lock (_sync)
        {
            before = _current;
            if (before == next) return;
            if (!Allowed[before].Contains(next))
            {
                Rejected?.Invoke(this, (before, next));
                throw new InvalidOperationException($"Invalid PointPilot run state transition: {before} -> {next}.");
            }
            _current = next;
        }
        Changed?.Invoke(this, next);
    }
}
