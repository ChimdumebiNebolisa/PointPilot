using PointPilot.Core.Elements;
using PointPilot.Core.Workflows;

namespace PointPilot.Core.Engine;

public sealed record BoundTarget(nint WindowHandle, uint ProcessId, string ProcessName, string Title);

public interface IForegroundMonitor
{
    nint GetForegroundHandle();
    bool IsWindowAlive(nint handle);
    bool IsWindowMinimized(nint handle);
    uint GetProcessId(nint handle);
    void SetForeground(nint handle);
}

public interface IInputPort
{
    Task ClickAsync(ScreenPoint point, ClickKind kind, RunLease lease, CancellationToken cancellationToken);
    Task TypeTextAsync(string text, RunLease lease, CancellationToken cancellationToken);
    Task PressKeysAsync(IReadOnlyList<string> keys, RunLease lease, CancellationToken cancellationToken);
}

public interface IScreenCapture
{
    /// <summary>Captures the window as PNG; when a clip region is supplied it is interpreted in window-relative coordinates.</summary>
    byte[] CapturePng(nint handle, WindowBounds? clipRegion = null);
}

public interface IImageComparer
{
    /// <summary>Compares two PNG images pixel by pixel; returns the fraction (0..1) of pixels within maxChannelDelta on every channel.</summary>
    double MatchFraction(byte[] actualPng, byte[] referencePng, int maxChannelDelta);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    Task Delay(int milliseconds, CancellationToken cancellationToken);
}

public interface IUiaSessionFactory
{
    /// <summary>Binds to a live window matching the target spec; throws StepFailureException with actionable diagnostics when ambiguous or absent.</summary>
    IUiaSession Bind(TargetSpec target, CancellationToken cancellationToken);
}
