using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace PointPilot.App;

internal sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001, ModControl = 0x0002, ModNoRepeat = 0x4000;
    private readonly HwndSource _source;
    private bool _escapeRegistered;

    public event EventHandler? ActivateRequested;
    public event EventHandler? StopRequested;

    public GlobalHotkeyService(nint windowHandle)
    {
        _source = HwndSource.FromHwnd(windowHandle) ?? throw new InvalidOperationException("Could not attach the global hotkey handler.");
        _source.AddHook(WndProc);
        if (!RegisterHotKey(windowHandle, 1, ModControl | ModAlt | ModNoRepeat, 0x20))
            throw new InvalidOperationException("Ctrl+Alt+Space is already registered by another application.");
    }

    public void SetEscapeEnabled(bool enabled)
    {
        if (enabled == _escapeRegistered) return;
        if (enabled && !RegisterHotKey(_source.Handle, 2, ModNoRepeat, 0x1B)) throw new InvalidOperationException("Escape could not be registered as the active stop key.");
        if (!enabled) UnregisterHotKey(_source.Handle, 2);
        _escapeRegistered = enabled;
    }

    private nint WndProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != WmHotkey) return 0;
        handled = true;
        if (wParam == 1) ActivateRequested?.Invoke(this, EventArgs.Empty);
        if (wParam == 2) StopRequested?.Invoke(this, EventArgs.Empty);
        return 0;
    }

    public void Dispose()
    {
        UnregisterHotKey(_source.Handle, 1);
        if (_escapeRegistered) UnregisterHotKey(_source.Handle, 2);
        _source.RemoveHook(WndProc);
    }

    [DllImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] private static extern bool RegisterHotKey(nint hWnd, int id, uint modifiers, uint virtualKey);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnregisterHotKey(nint hWnd, int id);
}
