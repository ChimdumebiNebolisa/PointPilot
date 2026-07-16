using System.Runtime.InteropServices;

namespace PointPilot.App;

internal sealed class ForegroundWindowTracker : IDisposable
{
    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutOfContext = 0x0000;
    private readonly WinEventDelegate _callback;
    private readonly nint _hook;
    private nint _lastExternalWindow;

    internal ForegroundWindowTracker()
    {
        _callback = OnForegroundChanged;
        _hook = SetWinEventHook(EventSystemForeground, EventSystemForeground, 0, _callback, 0, 0, WineventOutOfContext);
        if (_hook == 0) throw new InvalidOperationException("Could not track the previous foreground application.");
        var current = GetForegroundWindow();
        RecordIfExternal(current);
        for (var candidate = GetWindow(current, 2); _lastExternalWindow == 0 && candidate != 0; candidate = GetWindow(candidate, 2))
            if (IsWindowVisible(candidate)) RecordIfExternal(candidate);
    }

    internal bool RestoreIfPointPilotIsForeground()
    {
        var current = GetForegroundWindow();
        if (!BelongsToCurrentProcess(current)) return true;
        var target = Interlocked.CompareExchange(ref _lastExternalWindow, 0, 0);
        return target != 0 && IsWindow(target) && SetForegroundWindow(target);
    }

    private void OnForegroundChanged(nint hook, uint eventType, nint window, int objectId, int childId, uint eventThread, uint eventTime) => RecordIfExternal(window);

    private void RecordIfExternal(nint window)
    {
        if (window != 0 && IsWindow(window) && !BelongsToCurrentProcess(window)) Interlocked.Exchange(ref _lastExternalWindow, window);
    }

    private static bool BelongsToCurrentProcess(nint window)
    {
        _ = GetWindowThreadProcessId(window, out var processId);
        return processId == Environment.ProcessId;
    }

    public void Dispose()
    {
        if (_hook != 0) UnhookWinEvent(_hook);
        GC.KeepAlive(_callback);
    }

    private delegate void WinEventDelegate(nint hook, uint eventType, nint window, int objectId, int childId, uint eventThread, uint eventTime);
    [DllImport("user32.dll")] private static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint module, WinEventDelegate callback, uint processId, uint threadId, uint flags);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWinEvent(nint hook);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out int processId);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
}
