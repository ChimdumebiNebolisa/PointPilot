using PointPilot.Core;
using PointPilot.Core.Engine;
using PointPilot.Core.Elements;
using PointPilot.Core.Selectors;
using PointPilot.Core.Tracing;
using PointPilot.Core.Workflows;

namespace PointPilot.Tests;

internal sealed class FakeElement(ElementIdentity identity) : IUiElement
{
    public ElementIdentity Identity { get; init; } = identity;
    public bool IsEnabled { get; init; } = true;
    public bool IsOffscreen { get; init; }
    public WindowBounds Bounds { get; set; } = new(10, 10, 100, 30);
    public string? Value { get; init; }
    public int FocusCalls { get; private set; }
    public void Focus() => FocusCalls++;

    public static FakeElement Make(string? id = null, string? name = null, string? className = null, string? type = null) =>
        new(new ElementIdentity(id, name, className, type));
}

internal sealed class FakeSession : IUiaSession
{
    public FakeSession(nint handle = 0x1234, uint pid = 42, string processName = "Notepad", string title = "Untitled — Notepad")
    {
        WindowHandle = handle;
        ProcessId = pid;
        ProcessName = processName;
        Title = title;
    }

    public nint WindowHandle { get; }
    public uint ProcessId { get; }
    public string ProcessName { get; }
    public string Title { get; set; } = "";
    public WindowBounds LiveBounds { get; set; } = new(0, 0, 800, 600);
    public IReadOnlyList<IUiElement> Tree { get; set; } = [];
    public bool Disposed { get; private set; }

    public IUiElement RootElement => Tree[0];
    public IReadOnlyList<IUiElement> EnumerateSubtree() => Tree;
    public void Dispose() => Disposed = true;
}

internal sealed class FakeSessionFactory(FakeSession session) : IUiaSessionFactory
{
    public int BindCalls { get; private set; }
    public IUiaSession Bind(TargetSpec target, CancellationToken cancellationToken)
    {
        BindCalls++;
        return session;
    }
}

internal sealed class FakeMonitor : IForegroundMonitor
{
    public nint Foreground { get; set; } = 0x1234;
    public Dictionary<nint, bool> Alive { get; } = new();
    public Dictionary<nint, bool> Minimized { get; } = new();
    public Dictionary<nint, uint> ProcessIds { get; set; } = new() { [0x1234] = 42 };
    public List<nint> SetForegroundCalls { get; } = [];

    public nint GetForegroundHandle() => Foreground;
    public bool IsWindowAlive(nint handle) => Alive.TryGetValue(handle, out var alive) ? alive : true;
    public bool IsWindowMinimized(nint handle) => Minimized.TryGetValue(handle, out var min) && min;
    public uint GetProcessId(nint handle) => ProcessIds.TryGetValue(handle, out var pid) ? pid : 999;
    public void SetForeground(nint handle) { SetForegroundCalls.Add(handle); Foreground = handle; }
}

internal sealed class FakeInputPort : IInputPort
{
    public List<(ScreenPoint Point, ClickKind Kind)> Clicks { get; } = [];
    public List<string> Typed { get; } = [];
    public List<IReadOnlyList<string>> Pressed { get; } = [];
    public StepFailureException? ThrowOnAction { get; set; }

    public Task ClickAsync(ScreenPoint point, ClickKind kind, RunLease lease, CancellationToken cancellationToken)
    {
        lease.CancellationToken.ThrowIfCancellationRequested();
        if (ThrowOnAction is not null) throw ThrowOnAction;
        Clicks.Add((point, kind));
        return Task.CompletedTask;
    }

    public Task TypeTextAsync(string text, RunLease lease, CancellationToken cancellationToken)
    {
        lease.CancellationToken.ThrowIfCancellationRequested();
        if (ThrowOnAction is not null) throw ThrowOnAction;
        Typed.Add(text);
        return Task.CompletedTask;
    }

    public Task PressKeysAsync(IReadOnlyList<string> keys, RunLease lease, CancellationToken cancellationToken)
    {
        lease.CancellationToken.ThrowIfCancellationRequested();
        if (ThrowOnAction is not null) throw ThrowOnAction;
        Pressed.Add(keys);
        return Task.CompletedTask;
    }
}

internal sealed class FakeCapture : IScreenCapture
{
    public int Calls { get; private set; }
    public byte[] Bytes { get; set; } = [1, 2, 3, 4];
    public byte[] CapturePng(nint handle, WindowBounds? clipRegion = null)
    {
        Calls++;
        return Bytes;
    }
}

internal sealed class FakeImageComparer : IImageComparer
{
    public double Fraction { get; set; } = 1.0;
    public double MatchFraction(byte[] actualPng, byte[] referencePng, int maxChannelDelta) => Fraction;
}

/// <summary>Deterministic clock whose Delay advances virtual time by the requested amount.</summary>
internal sealed class ManualClock : IClock
{
    public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public DateTimeOffset UtcNow => Now;

    /// <summary>Raised just before virtual time advances; lets tests inject state changes deterministically.</summary>
    public Action<int>? Advancing { get; set; }

    public async Task Delay(int milliseconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        Advancing?.Invoke(milliseconds);
        Now = Now.AddMilliseconds(milliseconds);
    }
}

internal static class EngineTestHarness
{
    public static WorkflowDefinition Definition(params StepSpec[] steps) => new(
        SchemaVersion: 1,
        Name: "engine-test",
        Description: null,
        Variables: [],
        Target: new TargetSpec("Notepad", ProcessMatchMode.Exact, null),
        Defaults: new DefaultsSpec(5000),
        Steps: steps,
        SourcePath: "(test)",
        SourceHash: "abc");

    public static RunOptions Options(bool dryRun = false, string? output = null) => new(
        new Dictionary<string, string>(), dryRun, output,
        new MachineInfo("test-os", "8.0.0", true, 1920, 1080));
}
