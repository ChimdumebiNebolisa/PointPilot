using PointPilot.Core.Elements;
using PointPilot.Core.Selectors;
using PointPilot.Core.Tracing;
using PointPilot.Core.Workflows;

namespace PointPilot.Core.Engine;

public sealed record RunOptions(
    IReadOnlyDictionary<string, string> Variables,
    bool DryRun,
    string? OutputDirectory,
    MachineInfo Machine);

public sealed record RunResult(RunTrace Trace, string Summary);

/// <summary>
/// Executes a parsed workflow deterministically: bind once, resolve fresh before every
/// action, require the bound window foreground for every input-emitting step, evaluate
/// assertions as first-class steps, and stop the run on the first failed required step.
/// The same engine backs both the desktop app and the CLI.
/// </summary>
public sealed class WorkflowRunner(
    IUiaSessionFactory sessionFactory,
    IInputPort input,
    IForegroundMonitor monitor,
    IScreenCapture capture,
    IImageComparer images,
    IClock clock)
{
    public event EventHandler<(int StepIndex, int TotalSteps, string Status)>? Progress;

    public async Task<RunResult> ExecuteAsync(WorkflowDefinition workflow, RunOptions options, CancellationToken externalCancellation)
    {
        var machine = new RunStateMachine();
        var controller = new RunController();
        var started = clock.UtcNow;
        var steps = new List<StepTrace>();
        TargetRecord? targetRecord = null;
        IUiaSession? session = null;

        try
        {
            machine.Transition(RunState.Validating);
            var (resolved, substitutionDiagnostics) = VariableSubstituter.Resolve(workflow, options.Variables);
            if (substitutionDiagnostics.Count > 0)
                return FinishAsync(machine, controller, workflow, options, started, steps, targetRecord, RunState.Failed,
                    "Validation failed: " + string.Join(" ", substitutionDiagnostics.Select(d => d.ToString())));

            machine.Transition(RunState.Binding);
            session = sessionFactory.Bind(resolved.Target, externalCancellation);
            targetRecord = new TargetRecord(session.ProcessId, session.ProcessName, session.WindowHandle.ToInt64(), session.Title);
            machine.Transition(RunState.Running);

            for (var i = 0; i < resolved.Steps.Count; i++)
            {
                var step = resolved.Steps[i];
                Progress?.Invoke(this, (i, resolved.Steps.Count, "running"));
                var lease = controller.Lease;
                var stepStarted = clock.UtcNow;
                ResolvedElementRecord? resolvedRecord = null;
                var attempted = DescribeAttempt(step, options.DryRun);
                string? observed = null;
                string? evidencePath = null;
                try
                {
                    if (!controller.IsCurrent(lease)) throw new OperationCanceledException();
                    externalCancellation.ThrowIfCancellationRequested();

                    if (options.DryRun)
                    {
                        var (dryObserved, dryResolved) = await DryRunStepAsync(step, session, externalCancellation);
                        observed = dryObserved;
                        resolvedRecord = dryResolved;
                    }
                    else
                    {
                        using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(lease.CancellationToken, externalCancellation);
                        stepCts.CancelAfter(step.TimeoutMs ?? resolved.Defaults.TimeoutMs);
                        (attempted, observed, evidencePath, resolvedRecord) =
                            await ExecuteStepAsync(step, session, resolved.Target, lease, stepCts.Token, options);
                    }

                    steps.Add(new StepTrace(i, KindOf(step), step.Name, SelectorOf(step) is { } s ? SelectorResolver.Describe(s) : null,
                        resolvedRecord, attempted, observed, (int)(clock.UtcNow - stepStarted).TotalMilliseconds, StepStatus.Passed, null, evidencePath));
                    Progress?.Invoke(this, (i, resolved.Steps.Count, "passed"));
                }
                catch (StepFailureException ex)
                {
                    steps.Add(new StepTrace(i, KindOf(step), step.Name, SelectorOf(step) is { } s2 ? SelectorResolver.Describe(s2) : null,
                        resolvedRecord, attempted, observed, (int)(clock.UtcNow - stepStarted).TotalMilliseconds, StepStatus.Failed, ex.Message, evidencePath));
                    Progress?.Invoke(this, (i, resolved.Steps.Count, "failed"));
                    return FinishAsync(machine, controller, resolved, options, started, steps, targetRecord, RunState.Failed, $"Step {i + 1} ({KindOf(step)}) failed: {ex.Message}");
                }
                catch (OperationCanceledException)
                {
                    steps.Add(new StepTrace(i, KindOf(step), step.Name, null, resolvedRecord, attempted, observed,
                        (int)(clock.UtcNow - stepStarted).TotalMilliseconds, StepStatus.Skipped, "Cancelled before completion.", evidencePath));
                    return FinishAsync(machine, controller, resolved, options, started, steps, targetRecord, RunState.Cancelled, "The run was stopped before this step completed.");
                }
                catch (Exception ex)
                {
                    steps.Add(new StepTrace(i, KindOf(step), step.Name, SelectorOf(step) is { } s3 ? SelectorResolver.Describe(s3) : null,
                        resolvedRecord, attempted, observed, (int)(clock.UtcNow - stepStarted).TotalMilliseconds, StepStatus.Failed, $"Unexpected engine failure: {ex.Message}", evidencePath));
                    Progress?.Invoke(this, (i, resolved.Steps.Count, "failed"));
                    return FinishAsync(machine, controller, resolved, options, started, steps, targetRecord, RunState.Failed, $"Step {i + 1} ({KindOf(step)}) failed unexpectedly: {ex.Message}");
                }
            }

            return FinishAsync(machine, controller, resolved, options, started, steps, targetRecord, RunState.Completed, null);
        }
        catch (StepFailureException ex)
        {
            return FinishAsync(machine, controller, workflow, options, started, steps, targetRecord, RunState.Failed, ex.Message);
        }
        catch (OperationCanceledException)
        {
            return FinishAsync(machine, controller, workflow, options, started, steps, targetRecord, RunState.Cancelled, "The run was cancelled during binding.");
        }
        finally
        {
            session?.Dispose();
            controller.Dispose();
        }
    }

    private RunResult FinishAsync(
        RunStateMachine machine, RunController controller, WorkflowDefinition workflow, RunOptions options,
        DateTimeOffset started, List<StepTrace> steps, TargetRecord? target, RunState finalState, string? failure)
    {
        machine.Transition(finalState);
        var ended = clock.UtcNow;
        var status = finalState switch
        {
            RunState.Completed => "Completed",
            RunState.Failed => "Failed",
            _ => "Cancelled"
        };
        var trace = new RunTrace(controller.RunId, workflow.Name, workflow.SchemaVersion, workflow.SourceHash,
            status, started, ended, options.Machine, target, [.. steps], options.DryRun, failure);
        var summary = TraceSummarizer.Summarize(trace);
        PersistArtifacts(options, trace, summary);
        return new(trace, summary);
    }

    private void PersistArtifacts(RunOptions options, RunTrace trace, string summary)
    {
        if (options.OutputDirectory is null) return;
        var directory = Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(directory);
        var json = System.Text.Json.JsonSerializer.Serialize(trace, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        File.WriteAllText(Path.Combine(directory, "trace.json"), json);
        File.WriteAllText(Path.Combine(directory, "summary.txt"), summary);
    }

    private async Task<(string Attempted, string? Observed, string? Evidence, ResolvedElementRecord? Resolved)> ExecuteStepAsync(
        StepSpec step, IUiaSession session, TargetSpec target, RunLease lease, CancellationToken cancellationToken, RunOptions options)
    {
        switch (step)
        {
            case FocusWindowStep:
                {
                    EnsureTargetAlive(session);
                    monitor.SetForeground(session.WindowHandle);
                    await clock.Delay(150, cancellationToken);
                    VerifyForeground(session, target);
                    return ("Bring the bound window to the foreground.", $"Foreground is now '{session.Title}'.", null, null);
                }
            case ClickStep click:
                {
                    var (point, weak, resolved) = await ResolveClickPointAsync(click, session, cancellationToken);
                    VerifyForeground(session, target);
                    await input.ClickAsync(point, click.Kind, lease, cancellationToken);
                    return ($"{DescribeKind(click.Kind)} at ({point.X}, {point.Y})", $"Input sent to ({point.X}, {point.Y}).", null, MarkWeak(resolved, weak));
                }
            case TypeTextStep typeText:
                {
                    ResolvedElementRecord? record = null;
                    if (typeText.Selector is { } selector)
                    {
                        var element = ResolveRequired(session, selector);
                        record = Record(element, IsWeakSelector(typeText.Selector));
                        element.Focus();
                        await clock.Delay(80, cancellationToken);
                    }
                    VerifyForeground(session, target);
                    await input.TypeTextAsync(typeText.Text, lease, cancellationToken);
                    return ($"Type {typeText.Text.Length} characters", $"Typed text into {(typeText.Selector is null ? "focused control" : "resolved control")}.", null, record);
                }
            case PressStep press:
                {
                    VerifyForeground(session, target);
                    await input.PressKeysAsync(press.Keys, lease, cancellationToken);
                    return ($"Press {string.Join("+", press.Keys)}", "Key sequence sent.", null, null);
                }
            case FocusControlStep focusControl:
                {
                    var element = ResolveRequired(session, focusControl.Selector);
                    element.Focus();
                    return ("Set UI Automation focus.", $"Focused '{element.Identity.Name ?? element.Identity.AutomationId ?? element.Identity.ControlType}'.", null, Record(element, IsWeakSelector(focusControl.Selector)));
                }
            case WaitStep wait when wait.Condition is DelayMs delay:
                {
                    await clock.Delay(delay.Milliseconds, cancellationToken);
                    return ("Explicit bounded delay (declared in the workflow).", $"Waited {delay.Milliseconds} ms.", null, null);
                }
            case WaitStep wait:
                {
                    var deadline = clock.UtcNow + TimeSpan.FromMilliseconds(wait.TimeoutMs ?? 5000);
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (EvaluateCondition(wait.Condition, session) is { } observation && observation.Met)
                            return ("Wait for condition.", observation.Detail, null, null);
                        if (clock.UtcNow >= deadline)
                            throw new StepFailureException($"Timed out waiting for {DescribeCondition(wait.Condition)}. The condition never became true within the step timeout.");
                        await clock.Delay(Math.Min(200, Math.Max(1, (int)(deadline - clock.UtcNow).TotalMilliseconds)), cancellationToken);
                    }
                }
            case ScreenshotStep:
                {
                    EnsureTargetAlive(session);
                    var png = capture.CapturePng(session.WindowHandle);
                    var evidencePath = SaveEvidence(options, png);
                    return ("Capture diagnostic screenshot of the bound window.", $"Captured {png.Length} bytes.", evidencePath, null);
                }
            case AssertFileStep assertFile:
                {
                    var exists = File.Exists(assertFile.Path);
                    var ok = assertFile.Condition == FileCondition.Exists ? exists : !exists;
                    if (!ok)
                        throw new StepFailureException($"File assertion failed: '{assertFile.Path}' {(assertFile.Condition == FileCondition.Exists ? "does not exist" : "still exists")}.");
                    return ($"Assert file {assertFile.Condition} '{assertFile.Path}'", $"Verified the file {assertFile.Condition}.", null, null);
                }
            case AssertWindowStep assertWindow:
                {
                    var observedWindow = EvaluateWindowCondition(assertWindow.Condition, session);
                    if (!observedWindow.Met)
                        throw new StepFailureException($"Window assertion failed: expected {assertWindow.Condition}, observed {observedWindow.Detail}.");
                    return ($"Assert window {assertWindow.Condition}", observedWindow.Detail, null, null);
                }
            case AssertControlStep assertControl:
                {
                    var matches = SelectorResolver.FindAll(session.EnumerateSubtree(), ToCriteria(assertControl.Selector));
                    if (matches.Count == 0) throw new SelectorFailureException($"Selector {SelectorResolver.Describe(assertControl.Selector)} matched no elements.", new SelectorResolution.ZeroMatches());
                    if (matches.Count > 1 && assertControl.Selector.Pick is null)
                        throw new SelectorFailureException($"Selector {SelectorResolver.Describe(assertControl.Selector)} matched {matches.Count} elements; refine the selector or declare pick.", new SelectorResolution.Ambiguous([.. matches.Select(m => m.Identity)]));
                    var element = matches[0];
                    var stateOk = assertControl.State switch
                    {
                        ControlState.Exists => true,
                        ControlState.Visible => !element.IsOffscreen && element.Bounds.IsValid,
                        ControlState.Enabled => element.IsEnabled,
                        ControlState.Value => string.Equals(element.Value ?? "", assertControl.Value ?? "", StringComparison.Ordinal),
                        _ => false
                    };
                    if (!stateOk)
                        throw new StepFailureException($"Control assertion failed: expected state '{assertControl.State}'{(assertControl.State == ControlState.Value ? $"='{assertControl.Value}'" : "")}, observed {(assertControl.State == ControlState.Value ? $"'{element.Value ?? "<no value>"}'" : DescribeControlState(element))}.");
                    return ($"Assert control {assertControl.State}", $"Control satisfied state '{assertControl.State}'.", null, Record(element, IsWeakSelector(assertControl.Selector)));
                }
            case AssertImageStep assertImage:
                {
                    var element = ResolveRequired(session, assertImage.Selector);
                    EnsureTargetAlive(session);
                    var window = session.LiveBounds;
                    var relativeClip = new WindowBounds(element.Bounds.Left - window.Left, element.Bounds.Top - window.Top, element.Bounds.Width, element.Bounds.Height);
                    var png = capture.CapturePng(session.WindowHandle, relativeClip);
                    if (!File.Exists(assertImage.ReferenceImage))
                        throw new StepFailureException($"Reference image not found: {assertImage.ReferenceImage}.");
                    var reference = await File.ReadAllBytesAsync(assertImage.ReferenceImage, cancellationToken);
                    var fraction = images.MatchFraction(png, reference, assertImage.MaxChannelDelta);
                    if (fraction < 1.0)
                        throw new StepFailureException($"Image assertion failed: only {fraction:P1} of pixels match reference '{assertImage.ReferenceImage}' within a channel delta of {assertImage.MaxChannelDelta}.");
                    return ($"Compare element pixels with '{assertImage.ReferenceImage}' (delta <= {assertImage.MaxChannelDelta})", $"All sampled pixels matched ({fraction:P1}).", null, Record(element, IsWeakSelector(assertImage.Selector)));
                }
            default:
                throw new StepFailureException($"Unsupported step kind {step.GetType().Name}; the workflow parser should have rejected this definition.");
        }
    }

    private async Task<(string Observed, ResolvedElementRecord? Resolved)> DryRunStepAsync(StepSpec step, IUiaSession session, CancellationToken ct)
    {
        switch (step)
        {
            case ClickStep click:
                if (click.Selector.IsCoordinate) return ("Dry-run: coordinate click would be validated against live window bounds.", null);
                var element = ResolveRequired(session, click.Selector);
                return ($"Dry-run: would click '{DescribeIdentity(element)}'.", Record(element, IsWeakSelector(click.Selector)));
            case FocusControlStep f:
                var fe = ResolveRequired(session, f.Selector);
                return ($"Dry-run: would focus '{DescribeIdentity(fe)}'.", Record(fe, IsWeakSelector(f.Selector)));
            case TypeTextStep t when t.Selector is not null:
                var te = ResolveRequired(session, t.Selector);
                return ($"Dry-run: would focus and type into '{DescribeIdentity(te)}'.", Record(te, IsWeakSelector(t.Selector)));
            case ScreenshotStep:
                EnsureTargetAlive(session);
                return ("Dry-run: screenshot capability verified without saving.", null);
            default:
                await Task.CompletedTask;
                return ("Dry-run: skipped (would mutate or observe at run time).", null);
        }
    }

    private async Task<(ScreenPoint Point, bool Weak, ResolvedElementRecord Resolved)> ResolveClickPointAsync(ClickStep click, IUiaSession session, CancellationToken ct)
    {
        if (click.Selector.IsCoordinate)
        {
            var relative = new ScreenPoint(click.Selector.X!.Value, click.Selector.Y!.Value);
            var mapped = CoordinateMapper.RelativeToScreen(relative, session.LiveBounds);
            return (mapped, true, new ResolvedElementRecord(null, null, null, "coordinates", session.LiveBounds, true));
        }
        var element = ResolveRequired(session, click.Selector);
        if (element.IsOffscreen) throw new StepFailureException($"The resolved element '{DescribeIdentity(element)}' is off-screen.");
        if (!element.IsEnabled) throw new StepFailureException($"The resolved element '{DescribeIdentity(element)}' is disabled.");
        var point = CoordinateMapper.ClampIntoCenter(element.Bounds, session.LiveBounds);
        await Task.CompletedTask;
        return (point, IsWeakSelector(click.Selector), Record(element, IsWeakSelector(click.Selector)));
    }

    private IUiElement ResolveRequired(IUiaSession session, SelectorSpec spec)
    {
        var criteria = ToCriteria(spec);
        var subtree = session.EnumerateSubtree();
        var matches = SelectorResolver.FindAll(subtree, criteria);
        if (matches.Count == 0)
            throw new SelectorFailureException($"Selector {SelectorResolver.Describe(spec)} matched no elements in the target window. Searched {subtree.Count} elements.", new SelectorResolution.ZeroMatches());
        return ApplyPickOrThrow(spec, matches);
    }

    /// <summary>
    /// A target is weak when it relies on declared picks, raw coordinates, or property
    /// combinations without an automation ID or accessible name. Weakness is surfaced in
    /// traces so brittle steps are visible instead of silently trusted.
    /// </summary>
    public static bool IsWeakSelector(SelectorSpec spec) =>
        spec.Pick is not null || spec.IsCoordinate || (spec.AutomationId is null && spec.Name is null);

    private static IUiElement ApplyPickOrThrow(SelectorSpec spec, IReadOnlyList<IUiElement> matches)
    {
        if (spec.Pick is null)
        {
            if (matches.Count <= 1) return matches[0];
            throw new SelectorFailureException($"Selector {SelectorResolver.Describe(spec)} matched {matches.Count} elements; refine the selector or declare an explicit pick.", new SelectorResolution.Ambiguous([.. matches.Select(m => m.Identity)]));
        }
        return SelectorResolver.ApplyPick(spec, matches).Element;
    }

    private (bool Met, string Detail) EvaluateCondition(WaitCondition condition, IUiaSession session) => condition switch
    {
        WaitForWindowTitle w => SafeRegexIsMatch(w.Regex, session.Title) ? (true, $"Title became '{session.Title}'.") : (false, ""),
        WaitForControl w => EvaluateControlWait(w, session),
        WaitForFile f => (f.Condition == FileCondition.Exists ? File.Exists(f.Path) : !File.Exists(f.Path), $"File '{f.Path}' {f.Condition}."),
        _ => (false, "")
    };

    private static bool SafeRegexIsMatch(string pattern, string value)
    {
        try { return System.Text.RegularExpressions.Regex.IsMatch(value, pattern, System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1)); }
        catch (ArgumentException ex) { throw new StepFailureException($"Invalid wait regular expression '{pattern}': {ex.Message}"); }
    }

    private static (bool Met, string Detail) EvaluateControlWait(WaitForControl w, IUiaSession session)
    {
        var matches = SelectorResolver.FindAll(session.EnumerateSubtree(), ToCriteria(w.Selector));
        if (matches.Count == 0) return (false, "");
        var element = matches[0];
        var met = w.State switch
        {
            ControlState.Exists => true,
            ControlState.Visible => !element.IsOffscreen && element.Bounds.IsValid,
            ControlState.Enabled => element.IsEnabled,
            ControlState.Value => !string.IsNullOrEmpty(element.Value),
            _ => false
        };
        return met ? (true, $"Control '{DescribeIdentity(element)}' reached state {w.State}.") : (false, "");
    }

    private (bool Met, string Detail) EvaluateWindowCondition(WindowCondition condition, IUiaSession session)
    {
        var alive = monitor.IsWindowAlive(session.WindowHandle);
        return condition switch
        {
            WindowCondition.Closed => (!alive, alive ? "the window still exists" : "the window no longer exists"),
            WindowCondition.Visible => alive && !monitor.IsWindowMinimized(session.WindowHandle)
                ? (true, "the window is alive and not minimized")
                : (false, !alive ? "the window no longer exists" : "the window is minimized"),
            WindowCondition.Minimized => alive && monitor.IsWindowMinimized(session.WindowHandle)
                ? (true, "the window is minimized")
                : (false, !alive ? "the window no longer exists" : "the window is not minimized"),
            WindowCondition.Foreground => alive && monitor.GetForegroundHandle() == session.WindowHandle
                ? (true, "the bound window has foreground")
                : (false, !alive ? "the window no longer exists" : "another window has foreground"),
            _ => (false, "unknown condition")
        };
    }

    private void VerifyForeground(IUiaSession session, TargetSpec target)
    {
        EnsureTargetAlive(session);
        var foreground = monitor.GetForegroundHandle();
        if (foreground != session.WindowHandle)
            throw new StepFailureException($"The bound window lost foreground before input (foreground is 0x{foreground:x}). Run a focus-window step or bring '{target.ProcessName}' forward, then retry.");
        if (monitor.GetProcessId(foreground) != session.ProcessId)
            throw new StepFailureException($"The bound window's process changed (expected pid {session.ProcessId}); the target application may have restarted. Re-run to rebind.");
    }

    private static void EnsureTargetAlive(IUiaSession session)
    {
        if (!session.LiveBounds.IsValid)
            throw new StepFailureException($"The target window is gone or invalid (process '{session.ProcessName}', pid {session.ProcessId}). Restart it and re-run the workflow.");
    }

    private static SelectorCriteria ToCriteria(SelectorSpec spec) =>
        new(spec.AutomationId, spec.Name, spec.ClassName, spec.Role);

    private static ResolvedElementRecord Record(IUiElement element, bool weak) =>
        new(element.Identity.AutomationId, element.Identity.Name, element.Identity.ClassName, element.Identity.ControlType, element.Bounds, weak);

    private static ResolvedElementRecord? MarkWeak(ResolvedElementRecord? record, bool weak) =>
        record is null ? null : weak ? record with { WeakTarget = true } : record;

    private string SaveEvidence(RunOptions options, byte[] png)
    {
        if (options.OutputDirectory is null) return "(not persisted: no output directory)";
        Directory.CreateDirectory(options.OutputDirectory);
        var path = Path.Combine(Path.GetFullPath(options.OutputDirectory), $"evidence-{clock.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}.png");
        File.WriteAllBytes(path, png);
        return path;
    }

    public static string KindOf(StepSpec step) => step.GetType().Name.Replace("Step", "", StringComparison.Ordinal);
    public static SelectorSpec? SelectorOf(StepSpec step) => step switch
    {
        ClickStep c => c.Selector,
        FocusControlStep f => f.Selector,
        TypeTextStep t => t.Selector,
        AssertControlStep a => a.Selector,
        AssertImageStep i => i.Selector,
        _ => null
    };

    private static string DescribeAttempt(StepSpec step, bool dryRun) => step switch
    {
        FocusWindowStep => "Bring bound window to foreground",
        ClickStep c => $"{DescribeKind(c.Kind)}",
        TypeTextStep => "Type text",
        PressStep p => $"Press {string.Join("+", p.Keys)}",
        FocusControlStep => "Focus control",
        WaitStep w => $"Wait: {DescribeCondition(w.Condition)}",
        ScreenshotStep => "Capture diagnostic screenshot",
        AssertFileStep f => $"Assert file {f.Condition}",
        AssertWindowStep w => $"Assert window {w.Condition}",
        AssertControlStep a => $"Assert control {a.State}",
        AssertImageStep => "Assert image parity",
        _ => step.GetType().Name
    } + (dryRun ? " (dry-run)" : "");

    private static string DescribeKind(ClickKind kind) => kind switch
    {
        ClickKind.Double => "Double-click",
        ClickKind.Right => "Right-click",
        _ => "Click"
    };

    public static string DescribeCondition(WaitCondition condition) => condition switch
    {
        WaitForWindowTitle w => $"window title matches '{w.Regex}'",
        WaitForControl c => $"control {c.State}",
        WaitForFile f => $"file '{f.Path}' {f.Condition}",
        DelayMs d => $"delay {d.Milliseconds} ms",
        _ => condition.GetType().Name
    };

    private static string DescribeIdentity(IUiElement element)
    {
        var id = element.Identity;
        return id.Name ?? id.AutomationId ?? id.ControlType ?? "unnamed element";
    }

    private static string DescribeControlState(IUiElement element)
    {
        var flags = new List<string>();
        if (element.IsOffscreen) flags.Add("offscreen");
        if (!element.IsEnabled) flags.Add("disabled");
        if (!element.Bounds.IsValid) flags.Add("without valid bounds");
        return flags.Count == 0 ? "a normal visible enabled element" : string.Join(", ", flags);
    }
}
