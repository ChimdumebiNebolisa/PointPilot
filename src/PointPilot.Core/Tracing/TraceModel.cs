using System.Text;
using PointPilot.Core.Engine;

namespace PointPilot.Core.Tracing;

public sealed record MachineInfo(string OsVersion, string ClrVersion, bool Is64BitProcess, int ScreenWidth, int ScreenHeight);

public sealed record TargetRecord(uint ProcessId, string ProcessName, long WindowHandle, string Title);

public sealed record ResolvedElementRecord(
    string? AutomationId,
    string? Name,
    string? ClassName,
    string? ControlType,
    WindowBounds Bounds,
    bool WeakTarget);

public enum StepStatus { Passed, Failed, Skipped }

public sealed record StepTrace(
    int Index,
    string Kind,
    string? Name,
    string? RequestedSelector,
    ResolvedElementRecord? Resolved,
    string ActionAttempted,
    string? ObservedPostcondition,
    int DurationMs,
    StepStatus Status,
    string? FailureReason,
    string? EvidencePath);

public sealed record RunTrace(
    Guid RunId,
    string WorkflowName,
    int SchemaVersion,
    string WorkflowHash,
    string Status,
    DateTimeOffset StartedUtc,
    DateTimeOffset EndedUtc,
    MachineInfo Machine,
    TargetRecord? Target,
    IReadOnlyList<StepTrace> Steps,
    bool DryRun,
    string? FailureReason)
{
    public int Passed => Steps.Count(s => s.Status == StepStatus.Passed);
    public int FailedCount => Steps.Count(s => s.Status == StepStatus.Failed);
}

public static class TraceSummarizer
{
    public static string Summarize(RunTrace trace)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"PointPilot run {trace.Status}{(trace.DryRun ? " (dry-run)" : "")} — workflow '{trace.WorkflowName}'");
        sb.AppendLine($"Target: {(trace.Target is null ? "not bound" : $"{trace.Target.ProcessName} (pid {trace.Target.ProcessId}, hwnd 0x{trace.Target.WindowHandle:x}) '{trace.Target.Title}'")}");
        sb.AppendLine($"Steps: {trace.Passed} passed, {trace.FailedCount} failed, {trace.Steps.Count - trace.Passed - trace.FailedCount} skipped. Started {trace.StartedUtc:u}, ended {trace.EndedUtc:u}.");
        foreach (var step in trace.Steps)
        {
            sb.Append($"  [{step.Index + 1}] {step.Kind}{(string.IsNullOrWhiteSpace(step.Name) ? "" : $" '{step.Name}'")}: {step.Status}");
            if (step.DurationMs >= 0) sb.Append($" ({step.DurationMs} ms)");
            if (step.FailureReason is not null) sb.Append($" — {step.FailureReason}");
            if (step.EvidencePath is not null) sb.Append($" [evidence: {step.EvidencePath}]");
            sb.AppendLine();
        }
        if (trace.FailureReason is not null) sb.AppendLine($"Run failure: {trace.FailureReason}");
        return sb.ToString();
    }
}
