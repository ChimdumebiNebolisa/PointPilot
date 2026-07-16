namespace PointPilot.Core;

public sealed record TaskLease(Guid TaskId, long Revision, CancellationToken CancellationToken);
public sealed record ConfirmationTicket(Guid TaskId, long Revision, string Action, string? TargetPath, DateTimeOffset ConfirmedAt);
public sealed record CompletedAction(long Revision, string Description, DateTimeOffset CompletedAt);
public sealed record TaskSnapshot(Guid? TaskId, long Revision, string Goal, IReadOnlyList<string> Constraints, IReadOnlyList<CompletedAction> CompletedActions, bool IsCancelled, bool RequiresConfirmation, ConfirmationTicket? Confirmation);

public sealed class TaskCoordinator : ITaskLeaseValidator, IDisposable
{
    private readonly object _sync = new();
    private CancellationTokenSource? _cancellation;
    private Guid? _taskId;
    private long _revision;
    private string _goal = string.Empty;
    private readonly List<string> _constraints = [];
    private readonly List<CompletedAction> _completed = [];
    private bool _requiresConfirmation;
    private string? _confirmationAction;
    private string? _targetPath;
    private ConfirmationTicket? _confirmation;

    public TaskSnapshot Snapshot
    {
        get { lock (_sync) return CreateSnapshot(); }
    }

    public TaskSnapshot Start(string goal, IEnumerable<string>? constraints = null, bool requiresConfirmation = false, string? confirmationAction = null, string? targetPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goal);
        lock (_sync)
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            _taskId = Guid.NewGuid();
            _revision = 0;
            _goal = goal.Trim();
            _constraints.Clear();
            if (constraints is not null) _constraints.AddRange(constraints.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            _completed.Clear();
            _requiresConfirmation = requiresConfirmation;
            _confirmationAction = confirmationAction;
            _targetPath = targetPath;
            _confirmation = null;
            return CreateSnapshot();
        }
    }

    public TaskLease GetLease()
    {
        lock (_sync)
        {
            if (_taskId is null || _cancellation is null) throw new InvalidOperationException("No active computer task.");
            if (_requiresConfirmation && (_confirmation is null || _confirmation.Revision != _revision))
                throw new InvalidOperationException("The consequential action is not confirmed for the current task revision.");
            return new TaskLease(_taskId.Value, _revision, _cancellation.Token);
        }
    }

    public ConfirmationTicket Confirm(string action, string? targetPath)
    {
        lock (_sync)
        {
            if (_taskId is null) throw new InvalidOperationException("No active task to confirm.");
            if (!_requiresConfirmation) throw new InvalidOperationException("The active task does not require confirmation.");
            if (!string.Equals(action, _confirmationAction, StringComparison.Ordinal) || !string.Equals(targetPath, _targetPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Confirmation does not match the exact pending action and target.");
            _confirmation = new ConfirmationTicket(_taskId.Value, _revision, action, targetPath, DateTimeOffset.UtcNow);
            return _confirmation;
        }
    }

    public TaskSnapshot Interrupt(string correction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correction);
        lock (_sync)
        {
            if (_taskId is null) throw new InvalidOperationException("No active task to interrupt.");
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            _revision++;
            _constraints.Add(correction.Trim());
            _confirmation = null;
            return CreateSnapshot();
        }
    }

    public TaskSnapshot Revise(string revisedGoal, IEnumerable<string>? constraints = null, bool requiresConfirmation = false, string? confirmationAction = null, string? targetPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revisedGoal);
        lock (_sync)
        {
            if (_taskId is null) throw new InvalidOperationException("No active task to revise.");
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            _revision++;
            if (!string.Equals(_goal, revisedGoal.Trim(), StringComparison.OrdinalIgnoreCase))
                _constraints.Add($"Original goal to preserve: {_goal}");
            _goal = revisedGoal.Trim();
            if (constraints is not null) _constraints.AddRange(constraints.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            _requiresConfirmation = requiresConfirmation;
            _confirmationAction = confirmationAction;
            _targetPath = targetPath;
            _confirmation = null;
            return CreateSnapshot();
        }
    }

    public TaskSnapshot Pause()
    {
        lock (_sync)
        {
            _cancellation?.Cancel();
            _revision++;
            _confirmation = null;
            return CreateSnapshot();
        }
    }

    public TaskSnapshot Resume()
    {
        lock (_sync)
        {
            if (_taskId is null) throw new InvalidOperationException("No task to resume.");
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            return CreateSnapshot();
        }
    }

    public void RecordCompleted(TaskLease lease, string description)
    {
        lock (_sync)
        {
            if (!IsCurrentUnsafe(lease)) throw new OperationCanceledException("The action belongs to a stale task revision.");
            _completed.Add(new CompletedAction(_revision, description, DateTimeOffset.UtcNow));
        }
    }

    public bool IsCurrent(TaskLease lease)
    {
        lock (_sync) return IsCurrentUnsafe(lease);
    }

    public TaskSnapshot GetCurrentSnapshot(TaskLease lease)
    {
        lock (_sync)
        {
            if (!IsCurrentUnsafe(lease)) throw new OperationCanceledException("The task lease is stale.");
            return CreateSnapshot();
        }
    }

    private bool IsCurrentUnsafe(TaskLease lease) => _taskId == lease.TaskId && _revision == lease.Revision && _cancellation is not null && !_cancellation.IsCancellationRequested && !lease.CancellationToken.IsCancellationRequested;
    private TaskSnapshot CreateSnapshot() => new(_taskId, _revision, _goal, _constraints.ToArray(), _completed.ToArray(), _cancellation?.IsCancellationRequested ?? true, _requiresConfirmation, _confirmation);
    public void Dispose() { lock (_sync) { _cancellation?.Cancel(); _cancellation?.Dispose(); _cancellation = null; } }
}
