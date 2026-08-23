using System.Windows;
using System.Windows.Interop;
using PointPilot.Core;
using PointPilot.Core.Tracing;
using StepFailureException = PointPilot.Core.Elements.StepFailureException;

namespace PointPilot.App;

/// <summary>
/// Non-activating, click-through overlay used to flash the bounds of resolved targets
/// after a run. It never moves the system cursor, never takes focus, and never
/// intercepts input (WS_EX_TRANSPARENT | WS_EX_NOACTIVATE).
/// </summary>
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

    public void FlashResolved(RunTrace trace)
    {
        var lastResolved = trace.Steps.LastOrDefault(s => s.Resolved is not null)?.Resolved;
        if (lastResolved is null || !lastResolved.Bounds.IsValid) return;
        Left = lastResolved.Bounds.Left - 4;
        Top = lastResolved.Bounds.Top - 4;
        Width = Math.Max(28, lastResolved.Bounds.Width + 8);
        Height = Math.Max(28, lastResolved.Bounds.Height + 8);
        Show();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int GetWindowLong(nint hWnd, int index);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int SetWindowLong(nint hWnd, int index, int newStyle);
}
