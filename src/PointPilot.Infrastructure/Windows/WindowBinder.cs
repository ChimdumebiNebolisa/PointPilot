using System.Diagnostics;
using System.Text.RegularExpressions;
using PointPilot.Core;
using PointPilot.Core.Engine;
using PointPilot.Core.Elements;
using PointPilot.Core.Workflows;
using StepFailureException = PointPilot.Core.Elements.StepFailureException;

namespace PointPilot.Infrastructure.Windows;

/// <summary>
/// Binds a workflow target spec to exactly one live top-level window. Binding is
/// deterministic: the process name must match the declared mode, and when several
/// candidate windows remain (or none), binding fails with diagnostics instead of guessing.
/// </summary>
public sealed class WindowBinder : IUiaSessionFactory
{
    public IUiaSession Bind(TargetSpec target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var candidates = new List<(nint Handle, string Title, uint Pid)>();
        foreach (var process in Process.GetProcesses())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matches = target.ProcessNameMatch switch
            {
                ProcessMatchMode.Prefix => process.ProcessName.StartsWith(target.ProcessName, StringComparison.OrdinalIgnoreCase),
                _ => string.Equals(process.ProcessName, target.ProcessName, StringComparison.OrdinalIgnoreCase)
            };
            if (!matches) continue;
            var handle = process.MainWindowHandle;
            if (handle == 0) continue;
            if (!NativeMethods.IsWindow(handle)) continue;
            candidates.Add((handle, process.MainWindowTitle ?? "", (uint)process.Id));
        }

        if (target.WindowTitleRegex is { } pattern)
        {
            Regex regex;
            try { regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1)); }
            catch (ArgumentException ex) { throw new StepFailureException($"Invalid target windowTitleRegex: {ex.Message}"); }
            candidates = [.. candidates.Where(c => regex.IsMatch(c.Title))];
        }

        switch (candidates.Count)
        {
            case 0:
                throw new StepFailureException(
                    $"No running window matched target process '{target.ProcessName}'{(target.WindowTitleRegex is null ? "" : $" with title matching '{target.WindowTitleRegex}'")}. Start the application before running this workflow.");
            case > 1:
                throw new StepFailureException(
                    $"The target is ambiguous: {candidates.Count} windows match '{target.ProcessName}'. Add a windowTitleRegex to disambiguate: " +
                    string.Join("; ", candidates.Take(5).Select(c => $"'{c.Title}' (hwnd 0x{c.Handle:x})")));
        }

        return UiaSession.FromHandle(candidates[0].Handle);
    }
}
