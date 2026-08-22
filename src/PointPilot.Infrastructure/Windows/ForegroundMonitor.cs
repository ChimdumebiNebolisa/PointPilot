using System.Runtime.InteropServices;
using PointPilot.Core;
using PointPilot.Core.Engine;
using PointPilot.Core.Elements;

namespace PointPilot.Infrastructure.Windows;

public sealed class ForegroundMonitor : IForegroundMonitor
{
    public nint GetForegroundHandle() => NativeMethods.GetForegroundWindow();

    public bool IsWindowAlive(nint handle) => NativeMethods.IsWindow(handle);

    public bool IsWindowMinimized(nint handle) => NativeMethods.IsIconic(handle);

    public uint GetProcessId(nint handle)
    {
        _ = NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        return processId;
    }

    public void SetForeground(nint handle)
    {
        if (NativeMethods.SetForegroundWindow(handle)) return;

        // Windows denies foreground changes to processes without recent input rights.
        // A neutral ALT tap grants temporary rights; attaching to the active input
        // thread is the documented fallback. Both are attempted before failing closed.
        WindowsInputExecutor.SendKey(0x12, 0, 0);
        WindowsInputExecutor.SendKey(0x12, 0, NativeMethods.KeyUp);
        if (NativeMethods.SetForegroundWindow(handle)) return;

        var foregroundThread = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out _);
        var currentThread = NativeMethods.GetCurrentThreadId();
        var attached = false;
        try
        {
            attached = foregroundThread != 0 && NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
            if (NativeMethods.SetForegroundWindow(handle)) return;
        }
        finally
        {
            if (attached) NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
        }
        throw new StepFailureException("Windows refused to bring the target window to the foreground. Check that it is not minimized or on a locked desktop.");
    }

}
