using System.Windows;
using System.Windows.Interop;
using PointPilot.Core;

namespace PointPilot.App;

public partial class OverlayWindow : Window
{
    private const int ExTransparent = 0x20;
    private const int ExNoActivate = 0x08000000;
    private const int GwlExStyle = -20;

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            SetWindowLong(handle, GwlExStyle, GetWindowLong(handle, GwlExStyle) | ExTransparent | ExNoActivate);
        };
    }

    public void ShowTarget(WindowSnapshot snapshot, WindowBounds? imageTarget)
    {
        if (imageTarget is null) { Hide(); return; }
        var target = imageTarget.Value;
        var scaleX = snapshot.Bounds.Width / (double)snapshot.ImageWidth;
        var scaleY = snapshot.Bounds.Height / (double)snapshot.ImageHeight;
        Left = snapshot.Bounds.Left + target.Left * scaleX;
        Top = snapshot.Bounds.Top + target.Top * scaleY;
        Width = Math.Max(28, target.Width * scaleX);
        Height = Math.Max(28, target.Height * scaleY);
        Show();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int GetWindowLong(nint hWnd, int index);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int SetWindowLong(nint hWnd, int index, int newStyle);
}
