namespace PointPilot.Core;

public sealed record WindowSnapshot(
    nint Handle,
    string ProcessName,
    string Title,
    WindowBounds Bounds,
    int ImageWidth,
    int ImageHeight,
    byte[] PngBytes,
    ScreenPoint CursorPosition);

public sealed record VerificationResult(bool Succeeded, bool IsCertain, string Summary, string? Evidence = null)
{
    public static VerificationResult Uncertain(string summary) => new(false, false, summary);
}

public sealed record VisualGroundingResult(string Summary, WindowBounds? Target, bool IsCertain, string? ExpectedChange = null);
public sealed record ComputerRunResult(bool Completed, string Summary, int ActionsExecuted);
public sealed record FileCheckpoint(bool Existed, long Length, DateTimeOffset LastWriteTimeUtc);

public interface IWindowContextService { Task<WindowSnapshot> CaptureForegroundAsync(CancellationToken cancellationToken); }
public interface IComputerActionExecutor { Task ExecuteAsync(TaskLease lease, WindowSnapshot target, ComputerAction action, CancellationToken cancellationToken); }
public interface IComputerUseService { Task<ComputerRunResult> RunAsync(TaskLease lease, string goal, IReadOnlyList<string> constraints, CancellationToken cancellationToken); }
public interface IVisualReasoningService { Task<VisualGroundingResult> AnalyzeAsync(string request, WindowSnapshot snapshot, CancellationToken cancellationToken); }
public interface IVerificationService { Task<VerificationResult> VerifyAsync(string goal, WindowSnapshot before, WindowSnapshot after, string? expectedFilePath, FileCheckpoint? beforeFile, CancellationToken cancellationToken); }
public interface ITaskLeaseValidator { bool IsCurrent(TaskLease lease); TaskSnapshot GetCurrentSnapshot(TaskLease lease); }
