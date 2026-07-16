using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using PointPilot.Core;

namespace PointPilot.Infrastructure.Windows;

public sealed class WindowContextService : IWindowContextService
{
    public Task<WindowSnapshot> CaptureForegroundAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var handle = NativeMethods.GetForegroundWindow();
        if (handle == 0) throw new InvalidOperationException("No foreground window is available.");
        if (!NativeMethods.GetWindowRect(handle, out var rect)) throw new InvalidOperationException("The foreground window bounds could not be read.");
        var bounds = new WindowBounds(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        if (!bounds.IsValid) throw new InvalidOperationException("The foreground window has invalid or minimized bounds.");

        _ = NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        using var process = Process.GetProcessById(checked((int)processId));
        var png = CaptureWindow(handle, bounds);
        _ = NativeMethods.GetCursorPos(out var cursor);
        return Task.FromResult(new WindowSnapshot(handle, process.ProcessName, process.MainWindowTitle, bounds, bounds.Width, bounds.Height, png, new ScreenPoint(cursor.X, cursor.Y)));
    }

    private static byte[] CaptureWindow(nint handle, WindowBounds bounds)
    {
        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        var hdc = graphics.GetHdc();
        try
        {
            if (!NativeMethods.PrintWindow(handle, hdc, 2)) throw new InvalidOperationException("PrintWindow failed for the foreground target.");
        }
        finally { graphics.ReleaseHdc(hdc); }
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
