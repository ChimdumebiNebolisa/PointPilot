using PointPilot.Core;
using PointPilot.Core.Engine;using PointPilot.Core.Workflows;
using StepFailureException = PointPilot.Core.Elements.StepFailureException;

namespace PointPilot.Infrastructure.Windows;

/// <summary>
/// The only component allowed to emit real mouse/keyboard input (SendInput).
/// Actions are serialized; each action re-checks its run lease immediately before
/// sending, and any held modifiers or mouse buttons are released in finally blocks.
/// </summary>
public sealed class WindowsInputExecutor : IInputPort, IDisposable
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

    public async Task ClickAsync(ScreenPoint point, ClickKind kind, RunLease lease, CancellationToken cancellationToken)
    {
        await RunSerializedAsync(lease, cancellationToken, () =>
        {
            Move(point);
            switch (kind)
            {
                case ClickKind.Double:
                    PressMouse(left: true);
                    PressMouse(left: true);
                    break;
                case ClickKind.Right:
                    PressMouse(left: false, right: true);
                    break;
                default:
                    PressMouse(left: true);
                    break;
            }
        });
    }

    public async Task TypeTextAsync(string text, RunLease lease, CancellationToken cancellationToken)
    {
        await RunSerializedAsync(lease, cancellationToken, () =>
        {
            foreach (var ch in text)
            {
                SendKey(0, ch, NativeMethods.KeyUnicode);
                SendKey(0, ch, NativeMethods.KeyUnicode | NativeMethods.KeyUp);
            }
        });
    }

    public async Task PressKeysAsync(IReadOnlyList<string> keys, RunLease lease, CancellationToken cancellationToken)
    {
        await RunSerializedAsync(lease, cancellationToken, () =>
        {
            var virtualKeys = keys.Select(ToVirtualKey).ToArray();
            foreach (var key in virtualKeys) SendKey(key, 0, 0);
            try { return; }
            finally { foreach (var key in virtualKeys.Reverse()) SendKey(key, 0, NativeMethods.KeyUp); }
        });
    }

    private async Task RunSerializedAsync(RunLease lease, CancellationToken cancellationToken, Action action)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lease.CancellationToken, cancellationToken);
        await _serial.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (!lease.CancellationToken.IsCancellationRequested) action();
            linked.Token.ThrowIfCancellationRequested();
        }
        finally
        {
            _serial.Release();
        }
    }

    private static void PressMouse(bool left = false, bool right = false)
    {
        var downFlags = new List<uint>();
        var upFlags = new List<uint>();
        if (left) { downFlags.Add(NativeMethods.MouseLeftDown); upFlags.Add(NativeMethods.MouseLeftUp); }
        if (right) { downFlags.Add(NativeMethods.MouseRightDown); upFlags.Add(NativeMethods.MouseRightUp); }
        try
        {
            foreach (var flag in downFlags) SendMouse(flag);
        }
        finally
        {
            // Release every pressed button even when a press is rejected mid-way.
            foreach (var flag in upFlags) SendMouse(flag);
        }
    }

    private static void Move(ScreenPoint point)
    {
        if (!NativeMethods.SetCursorPos(point.X, point.Y))
            throw new StepFailureException($"Windows refused to move the pointer to ({point.X}, {point.Y}).");
    }

    private static void SendMouse(uint flags) =>
        Send([new NativeMethods.Input { Type = 0, Data = new NativeMethods.InputUnion { Mouse = new NativeMethods.MouseInput { Flags = flags } } }]);

    internal static void SendKey(ushort virtualKey, ushort scan, uint flags) =>
        Send([new NativeMethods.Input { Type = 1, Data = new NativeMethods.InputUnion { Keyboard = new NativeMethods.KeyboardInput { VirtualKey = virtualKey, ScanCode = scan, Flags = flags } } }]);

    private static void Send(NativeMethods.Input[] inputs)
    {
        if (NativeMethods.SendInputSafe(inputs) != inputs.Length)
            throw new StepFailureException("Windows rejected the input action.");
    }

    internal static ushort ToVirtualKey(string key)
    {
        var normalized = KeyNormalizer.Normalize(key);
        if (SpecialKeys.TryGetValue(normalized, out var value)) return value;
        if (normalized.Length == 1) return unchecked((ushort)(NativeMethods.VkKeyScan(normalized[0]) & 0xff));
        throw new StepFailureException($"Unsupported key name '{key}'. Use Windows key names such as ENTER, TAB, ESCAPE, or single characters.");
    }

    public void Dispose() => _serial.Dispose();
}
