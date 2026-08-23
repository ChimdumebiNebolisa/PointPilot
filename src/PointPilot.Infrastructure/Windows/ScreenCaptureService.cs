using System.Drawing;using System.IO;
using System.Drawing.Imaging;
using PointPilot.Core;
using PointPilot.Core.Engine;
using StepFailureException = PointPilot.Core.Elements.StepFailureException;

namespace PointPilot.Infrastructure.Windows;

/// <summary>
/// Captures a specific HWND via PrintWindow (PW_RENDERFULLCONTENT) — never the whole
/// desktop. An optional window-relative clip region supports element-scoped evidence.
/// </summary>
public sealed class ScreenCaptureService : IScreenCapture
{
    public byte[] CapturePng(nint handle, WindowBounds? clipRegion = null)
    {
        if (!NativeMethods.GetWindowRect(handle, out var rect))
            throw new StepFailureException("The target window bounds could not be read for capture; the window may be closing.");
        var full = new WindowBounds(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        if (!full.IsValid) throw new StepFailureException("The target window has invalid or minimized bounds and cannot be captured.");

        var region = clipRegion is null ? full : Clamp(clipRegion.Value, full);
        using var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var hdc = graphics.GetHdc();
            try
            {
                // When clipping, capture the full window into a temp bitmap first because PrintWindow always paints the whole window.
                if (clipRegion is null)
                {
                    if (!NativeMethods.PrintWindow(handle, hdc, 2)) throw new StepFailureException("PrintWindow failed for the target window.");
                }
                else
                {
                    using var fullBitmap = new Bitmap(full.Width, full.Height, PixelFormat.Format32bppArgb);
                    using (var fullGraphics = Graphics.FromImage(fullBitmap))
                    {
                        var fullHdc = fullGraphics.GetHdc();
                        try
                        {
                            if (!NativeMethods.PrintWindow(handle, fullHdc, 2)) throw new StepFailureException("PrintWindow failed for the target window.");
                        }
                        finally { fullGraphics.ReleaseHdc(fullHdc); }
                    }
                    graphics.DrawImage(fullBitmap, new Rectangle(0, 0, region.Width, region.Height), new Rectangle(region.Left - full.Left, region.Top - full.Top, region.Width, region.Height), GraphicsUnit.Pixel);
                }
            }
            finally { graphics.ReleaseHdc(hdc); }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static WindowBounds Clamp(WindowBounds region, WindowBounds window) =>
        new(
            Math.Clamp(region.Left, 0, window.Width),
            Math.Clamp(region.Top, 0, window.Height),
            Math.Clamp(region.Width, 1, window.Width),
            Math.Clamp(region.Height, 1, window.Height));
}
