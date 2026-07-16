using System.Diagnostics;
using PointPilot.Core;

namespace PointPilot.Infrastructure.Windows;

public sealed class WindowsInputExecutor(ITaskLeaseValidator leases) : IComputerActionExecutor, IDisposable
{
    private readonly SemaphoreSlim _serial = new(1, 1);
    private static readonly IReadOnlyDictionary<string, ushort> SpecialKeys = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
    {
        ["BACKSPACE"] = 0x08,
        ["TAB"] = 0x09,
        ["ENTER"] = 0x0D,
        ["SHIFT"] = 0x10,
        ["CTRL"] = 0x11,
        ["ALT"] = 0x12,
        ["ESCAPE"] = 0x1B,
        ["SPACE"] = 0x20,
        ["PAGEUP"] = 0x21,
        ["PAGEDOWN"] = 0x22,
        ["END"] = 0x23,
        ["HOME"] = 0x24,
        ["LEFT"] = 0x25,
        ["UP"] = 0x26,
        ["RIGHT"] = 0x27,
        ["DOWN"] = 0x28,
        ["DELETE"] = 0x2E,
        ["WIN"] = 0x5B
    };

    public async Task ExecuteAsync(TaskLease lease, WindowSnapshot target, ComputerAction action, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lease.CancellationToken, cancellationToken);
        await _serial.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            EnsureCurrent(lease);
            if (action.Type is not ComputerActionType.Screenshot and not ComputerActionType.Wait) ValidateTarget(target);
            if (action.Type == ComputerActionType.TypeText) ValidateConsequentialText(lease, target, action.Text ?? string.Empty);
            await ExecuteCoreAsync(action, target, linked.Token).ConfigureAwait(false);
        }
        finally { _serial.Release(); }
    }

    private async Task ExecuteCoreAsync(ComputerAction action, WindowSnapshot target, CancellationToken cancellationToken)
    {
        ScreenPoint Point() => CoordinateMapper.ImageToScreen(new ScreenPoint(action.X ?? throw new InvalidOperationException("Action X is required."), action.Y ?? throw new InvalidOperationException("Action Y is required.")), target.ImageWidth, target.ImageHeight, target.Bounds);
        switch (action.Type)
        {
            case ComputerActionType.Screenshot: return;
            case ComputerActionType.Wait: await Task.Delay(Math.Clamp(action.WaitMilliseconds == 0 ? 2000 : action.WaitMilliseconds, 0, 10_000), cancellationToken).ConfigureAwait(false); return;
            case ComputerActionType.Move: await WithModifiersAsync(action.Modifiers, () => { Move(Point()); return Task.CompletedTask; }).ConfigureAwait(false); return;
            case ComputerActionType.Click: await WithModifiersAsync(action.Modifiers, () => { Click(Point(), false, false); return Task.CompletedTask; }).ConfigureAwait(false); return;
            case ComputerActionType.DoubleClick: await WithModifiersAsync(action.Modifiers, () => { Click(Point(), false, true); return Task.CompletedTask; }).ConfigureAwait(false); return;
            case ComputerActionType.RightClick: await WithModifiersAsync(action.Modifiers, () => { Click(Point(), true, false); return Task.CompletedTask; }).ConfigureAwait(false); return;
            case ComputerActionType.MouseDown: Move(Point()); SendMouse(NativeMethods.MouseLeftDown); return;
            case ComputerActionType.MouseUp: if (action.X.HasValue && action.Y.HasValue) Move(Point()); SendMouse(NativeMethods.MouseLeftUp); return;
            case ComputerActionType.Scroll:
                await WithModifiersAsync(action.Modifiers, () =>
                {
                    if (action.X.HasValue && action.Y.HasValue) Move(Point());
                    if (action.ScrollY != 0) SendMouse(NativeMethods.MouseWheel, unchecked((uint)action.ScrollY));
                    if (action.ScrollX != 0) SendMouse(NativeMethods.MouseHWheel, unchecked((uint)action.ScrollX));
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
                return;
            case ComputerActionType.TypeText: TypeText(action.Text ?? string.Empty); return;
            case ComputerActionType.Keypress: Keypress(action.Keys ?? throw new InvalidOperationException("Keypress keys are required.")); return;
            case ComputerActionType.Drag:
                var path = action.Path ?? throw new InvalidOperationException("Drag path is required.");
                if (path.Count < 2) throw new InvalidOperationException("Drag requires at least two points.");
                await WithModifiersAsync(action.Modifiers, async () =>
                {
                    Move(CoordinateMapper.ImageToScreen(path[0], target.ImageWidth, target.ImageHeight, target.Bounds));
                    SendMouse(NativeMethods.MouseLeftDown);
                    try
                    {
                        foreach (var point in path.Skip(1)) { cancellationToken.ThrowIfCancellationRequested(); Move(CoordinateMapper.ImageToScreen(point, target.ImageWidth, target.ImageHeight, target.Bounds)); await Task.Delay(16, cancellationToken).ConfigureAwait(false); }
                    }
                    finally { SendMouse(NativeMethods.MouseLeftUp); }
                }).ConfigureAwait(false);
                return;
            default: throw new NotSupportedException($"Unsupported computer action: {action.Type}.");
        }
    }

    private void EnsureCurrent(TaskLease lease) { if (!leases.IsCurrent(lease)) throw new OperationCanceledException("The computer action belongs to a stale task revision."); }

    private void ValidateConsequentialText(TaskLease lease, WindowSnapshot target, string text)
    {
        var targetPath = leases.GetCurrentSnapshot(lease).Confirmation?.TargetPath;
        TargetWindowPolicy.ValidateConfirmedText(targetPath, target.Title, text);
    }

    private static async Task WithModifiersAsync(IReadOnlyList<string>? modifiers, Func<Task> action)
    {
        var keys = modifiers?.Select(ToVirtualKey).ToArray() ?? [];
        foreach (var key in keys) SendKey(key, 0, 0);
        try { await action().ConfigureAwait(false); }
        finally { foreach (var key in keys.Reverse()) SendKey(key, 0, NativeMethods.KeyUp); }
    }

    private static void ValidateTarget(WindowSnapshot target)
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground != target.Handle) throw new InvalidOperationException("The foreground window changed before input execution.");
        _ = NativeMethods.GetWindowThreadProcessId(foreground, out var pid);
        using var process = Process.GetProcessById(checked((int)pid));
        if (!NativeMethods.GetWindowRect(foreground, out var rect)) throw new InvalidOperationException("The foreground window bounds could not be read.");
        TargetWindowPolicy.ValidateForMutation(target, foreground, process.ProcessName, new WindowBounds(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top));
    }

    private static void Move(ScreenPoint point) { if (!NativeMethods.SetCursorPos(point.X, point.Y)) throw new InvalidOperationException("Pointer movement failed."); }
    private static void Click(ScreenPoint point, bool right, bool twice) { Move(point); var down = right ? NativeMethods.MouseRightDown : NativeMethods.MouseLeftDown; var up = right ? NativeMethods.MouseRightUp : NativeMethods.MouseLeftUp; SendMouse(down); SendMouse(up); if (twice) { SendMouse(down); SendMouse(up); } }
    private static void SendMouse(uint flags, uint data = 0) => Send([new NativeMethods.Input { Type = 0, Data = new NativeMethods.InputUnion { Mouse = new NativeMethods.MouseInput { Flags = flags, MouseData = data } } }]);
    private static void TypeText(string text) { foreach (var ch in text) { SendKey(0, ch, NativeMethods.KeyUnicode); SendKey(0, ch, NativeMethods.KeyUnicode | NativeMethods.KeyUp); } }
    private static void Keypress(IReadOnlyList<string> keys)
    {
        var virtualKeys = keys.Select(ToVirtualKey).ToArray();
        foreach (var key in virtualKeys) SendKey(key, 0, 0);
        foreach (var key in virtualKeys.Reverse()) SendKey(key, 0, NativeMethods.KeyUp);
    }
    private static ushort ToVirtualKey(string key) { var normalized = KeyNormalizer.Normalize(key); if (SpecialKeys.TryGetValue(normalized, out var value)) return value; if (normalized.Length == 1) return unchecked((ushort)(NativeMethods.VkKeyScan(normalized[0]) & 0xff)); throw new NotSupportedException($"Unsupported key: {normalized}."); }
    private static void SendKey(ushort virtualKey, ushort scan, uint flags) => Send([new NativeMethods.Input { Type = 1, Data = new NativeMethods.InputUnion { Keyboard = new NativeMethods.KeyboardInput { VirtualKey = virtualKey, ScanCode = scan, Flags = flags } } }]);
    private static void Send(NativeMethods.Input[] inputs) { if (NativeMethods.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.Input>()) != (uint)inputs.Length) throw new InvalidOperationException("Windows rejected an input action."); }
    public void Dispose() => _serial.Dispose();
}
