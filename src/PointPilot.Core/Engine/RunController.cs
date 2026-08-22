namespace PointPilot.Core.Engine;

public sealed record RunLease(Guid RunId, CancellationToken CancellationToken);

/// <summary>
/// Guards one workflow run. Every atomic action re-checks its lease immediately before
/// it is sent; stopping the run cancels the token so any queued action fails closed at
/// the next boundary instead of executing against stale intent.
/// </summary>
public sealed class RunController : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    public Guid RunId { get; } = Guid.NewGuid();

    public RunLease Lease => new(RunId, _cancellation.Token);

    /// <summary>Stops the run at the next atomic boundary. Idempotent.</summary>
    public void Stop()
    {
        try { _cancellation.Cancel(); }
        catch (ObjectDisposedException) { /* already disposed */ }
    }

    public bool IsCurrent(RunLease lease) =>
        lease.RunId == RunId && !lease.CancellationToken.IsCancellationRequested;

    public void Dispose()
    {
        try { _cancellation.Cancel(); } catch (ObjectDisposedException) { }
        _cancellation.Dispose();
    }
}
