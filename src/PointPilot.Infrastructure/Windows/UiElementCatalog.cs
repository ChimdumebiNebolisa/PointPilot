using System.Diagnostics;

namespace PointPilot.Infrastructure.Windows;

/// <summary>Read-only snapshot of one selectable top-level window.</summary>
public sealed record TopLevelWindowInfo(nint Handle, uint ProcessId, string ProcessName, string Title)
{
    public string DisplayName => $"{ProcessName} — {(string.IsNullOrWhiteSpace(Title) ? "(untitled)" : Title)}";
}

/// <summary>Lists candidate target windows for binding or recording.</summary>
public sealed class UiElementCatalog
{
    public IReadOnlyList<TopLevelWindowInfo> ListTopLevelWindows()
    {
        var results = new List<TopLevelWindowInfo>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var handle = process.MainWindowHandle;
                if (handle == 0) continue;
                if (!NativeMethods.IsWindow(handle)) continue;
                if (!NativeMethods.GetWindowRect(handle, out var rect)) continue;
                if (rect.Right - rect.Left <= 0 || rect.Bottom - rect.Top <= 0) continue;
                results.Add(new TopLevelWindowInfo(handle, (uint)process.Id, process.ProcessName, process.MainWindowTitle ?? ""));
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Access-denied processes (system services) have no selectable window anyway.
            }
        }
        return [.. results.OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)];
    }
}
