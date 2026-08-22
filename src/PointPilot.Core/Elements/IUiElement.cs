using PointPilot.Core.Selectors;

namespace PointPilot.Core.Elements;

public interface IUiElement
{
    ElementIdentity Identity { get; }
    bool IsEnabled { get; }
    bool IsOffscreen { get; }
    WindowBounds Bounds { get; }
    string? Value { get; }
    void Focus();
}

/// <summary>
/// A live binding to one target window's UI Automation tree. Implementations must
/// materialize fresh state on every call; cached elements are never reused across steps.
/// </summary>
public interface IUiaSession : IDisposable
{
    nint WindowHandle { get; }
    uint ProcessId { get; }
    string ProcessName { get; }
    string Title { get; }
    WindowBounds LiveBounds { get; }
    IUiElement RootElement { get; }
    IReadOnlyList<IUiElement> EnumerateSubtree();
}

public class StepFailureException : Exception
{
    public StepFailureException(string message) : base(message) { }
    public StepFailureException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class SelectorFailureException(string message, SelectorResolution resolution) : StepFailureException(message)
{
    public SelectorResolution Resolution { get; } = resolution;
}
