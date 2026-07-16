namespace PointPilot.Core;

public sealed record WorkflowOutcome(
    string Summary,
    WindowSnapshot? Snapshot = null,
    WindowBounds? Target = null,
    VerificationResult? Verification = null,
    bool RequiresConfirmation = false,
    string? ConfirmationAction = null,
    string? TargetPath = null);

public sealed class PointPilotWorkflow(
    PointPilotStateMachine state,
    TaskCoordinator tasks,
    IWindowContextService windows,
    IVisualReasoningService visual,
    IComputerUseService computer,
    IVerificationService verification)
{
    private string? _guidedGoal;
    private string? _pendingGuideChange;
    private readonly List<string> _completedGuideChanges = [];

    public async Task<WorkflowOutcome> TeachAsync(string request, CancellationToken cancellationToken)
    {
        BeginUnderstanding();
        var snapshot = await windows.CaptureForegroundAsync(cancellationToken).ConfigureAwait(false);
        state.Transition(PointPilotState.Teaching);
        var result = await visual.AnalyzeAsync(request, snapshot, cancellationToken).ConfigureAwait(false);
        state.Transition(PointPilotState.Speaking);
        return new(result.Summary, snapshot, result.Target);
    }

    public async Task<WorkflowOutcome> GuideAsync(string goal, string expectedChange, CancellationToken cancellationToken)
    {
        BeginUnderstanding();
        var snapshot = await windows.CaptureForegroundAsync(cancellationToken).ConfigureAwait(false);
        state.Transition(PointPilotState.Guiding);
        var continuing = string.Equals(_guidedGoal, goal, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(_pendingGuideChange);
        var request = continuing
            ? $"First verify whether this expected visible change occurred: {_pendingGuideChange}. If it did, give exactly one next step toward: {goal}. If it did not, explain only how to complete the pending step. Point to the relevant control."
            : $"Give exactly one visible, beginner-friendly next step toward: {goal}. Point to the control to use.";
        var result = await visual.AnalyzeAsync(request, snapshot, cancellationToken).ConfigureAwait(false);
        if (continuing && result.IsCertain) _completedGuideChanges.Add(_pendingGuideChange!);
        if (!string.Equals(_guidedGoal, goal, StringComparison.OrdinalIgnoreCase)) _completedGuideChanges.Clear();
        _guidedGoal = goal;
        _pendingGuideChange = result.ExpectedChange ?? expectedChange;
        state.Transition(PointPilotState.Speaking);
        return new(result.Summary, snapshot, result.Target);
    }

    public async Task<WorkflowOutcome> ActAsync(string goal, IReadOnlyList<string> constraints, string? targetPath, CancellationToken cancellationToken)
    {
        BeginUnderstanding();
        (goal, constraints) = MergeGuideContext(goal, constraints);
        var policy = ActionPolicy.ClassifyGoal(goal);
        if (policy == ActionPolicyLevel.Prohibited)
        {
            state.Transition(PointPilotState.Speaking);
            return new("I can’t perform that action. PointPilot is limited to the verified foreground GIMP workflow.");
        }

        var consequential = policy == ActionPolicyLevel.Consequential;
        var normalizedPath = NormalizePath(targetPath);
        if (consequential && normalizedPath is null)
        {
            state.Transition(PointPilotState.Speaking);
            return new("I need an exact export or save path before I can request confirmation.");
        }
        if (consequential && !string.Equals(Path.GetExtension(normalizedPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            state.Transition(PointPilotState.Speaking);
            return new("The verified consequential workflow supports only an exact PNG export path.");
        }
        var action = consequential ? "Export the current GIMP composition as PNG" : null;
        tasks.Start(goal, constraints, consequential, action, normalizedPath);
        state.Transition(PointPilotState.Planning);
        if (consequential)
            return new($"Confirmation required before export to {normalizedPath}.", RequiresConfirmation: true, ConfirmationAction: action, TargetPath: normalizedPath);

        return await ExecuteCurrentAsync(null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkflowOutcome> ReviseActAsync(string goal, IReadOnlyList<string> constraints, string? targetPath, CancellationToken cancellationToken)
    {
        BeginUnderstanding();
        (goal, constraints) = MergeGuideContext(goal, constraints);
        var policy = ActionPolicy.ClassifyGoal(goal);
        if (policy == ActionPolicyLevel.Prohibited)
        {
            state.Transition(PointPilotState.Speaking);
            return new("I can’t apply that correction. The paused safe steps are preserved.");
        }
        var consequential = policy == ActionPolicyLevel.Consequential;
        var normalizedPath = NormalizePath(targetPath);
        if (consequential && normalizedPath is null)
        {
            state.Transition(PointPilotState.Speaking);
            return new("I preserved the completed steps, but I need the exact path before export confirmation.");
        }
        if (consequential && !string.Equals(Path.GetExtension(normalizedPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            state.Transition(PointPilotState.Speaking);
            return new("I preserved the completed steps, but the verified workflow supports only a PNG export path.");
        }
        var action = consequential ? "Export the current GIMP composition as PNG" : null;
        tasks.Revise(goal, constraints, consequential, action, normalizedPath);
        state.Transition(PointPilotState.Planning);
        if (consequential)
            return new($"I preserved the completed steps. Confirmation is required before export to {normalizedPath}.", RequiresConfirmation: true, ConfirmationAction: action, TargetPath: normalizedPath);
        return await ExecuteCurrentAsync(null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkflowOutcome> ConfirmAndExecuteAsync(string action, string? targetPath, CancellationToken cancellationToken)
    {
        tasks.Confirm(action, NormalizePath(targetPath));
        return await ExecuteCurrentAsync(NormalizePath(targetPath), cancellationToken).ConfigureAwait(false);
    }

    public Task<WorkflowOutcome> UndoAsync(CancellationToken cancellationToken) =>
        ActAsync("Undo the most recent reversible GIMP edit using Ctrl+Z and stop after the visible change", ["Do not undo an export or overwrite."], null, cancellationToken);

    private async Task<WorkflowOutcome> ExecuteCurrentAsync(string? expectedFilePath, CancellationToken cancellationToken)
    {
        var snapshot = tasks.Snapshot;
        var lease = tasks.GetLease();
        var before = await windows.CaptureForegroundAsync(lease.CancellationToken).ConfigureAwait(false);
        var beforeFile = CaptureFileCheckpoint(expectedFilePath);
        state.Transition(PointPilotState.Acting);
        var goal = expectedFilePath is null
            ? snapshot.Goal
            : $"{snapshot.Goal}. Explicit confirmation was granted for {expectedFilePath}; do not use a different path.";
        var run = await computer.RunAsync(lease, goal, snapshot.Constraints, cancellationToken).ConfigureAwait(false);
        state.Transition(PointPilotState.Verifying);
        var after = await windows.CaptureForegroundAsync(lease.CancellationToken).ConfigureAwait(false);
        var verified = run.Completed
            ? await verification.VerifyAsync(snapshot.Goal, before, after, expectedFilePath, beforeFile, cancellationToken).ConfigureAwait(false)
            : VerificationResult.Uncertain(run.Summary);
        if (verified.Succeeded) tasks.RecordCompleted(lease, snapshot.Goal);
        state.Transition(PointPilotState.Speaking);
        var summary = verified.Succeeded
            ? $"Done and verified. {verified.Summary}"
            : $"I stopped without claiming completion. {verified.Summary}";
        return new(summary, after, Verification: verified);
    }

    private void BeginUnderstanding()
    {
        if (state.Current == PointPilotState.Speaking) state.Transition(PointPilotState.Listening);
        if (state.Current == PointPilotState.Error) state.Transition(PointPilotState.Listening);
        if (state.Current != PointPilotState.Listening)
            throw new InvalidOperationException($"A new request cannot start while PointPilot is {state.Current}.");
        state.Transition(PointPilotState.Understanding);
    }

    private static string? NormalizePath(string? path) => string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));

    private static FileCheckpoint? CaptureFileCheckpoint(string? path)
    {
        if (path is null) return null;
        var file = new FileInfo(path);
        return file.Exists ? new FileCheckpoint(true, file.Length, file.LastWriteTimeUtc) : new FileCheckpoint(false, 0, DateTimeOffset.MinValue);
    }

    private (string Goal, IReadOnlyList<string> Constraints) MergeGuideContext(string goal, IReadOnlyList<string> constraints)
    {
        if (string.IsNullOrWhiteSpace(_guidedGoal)) return (goal, constraints);
        var merged = constraints.ToList();
        if (!string.Equals(goal, _guidedGoal, StringComparison.OrdinalIgnoreCase)) merged.Add($"Preserve the guided goal: {_guidedGoal}");
        foreach (var completed in _completedGuideChanges) merged.Add($"Already completed in Guide mode: {completed}");
        var effectiveGoal = IsGenericModeSwitch(goal) ? _guidedGoal : goal;
        _guidedGoal = null;
        _pendingGuideChange = null;
        _completedGuideChanges.Clear();
        return (effectiveGoal, merged);
    }

    private static bool IsGenericModeSwitch(string goal) =>
        goal.Trim().Equals("do it", StringComparison.OrdinalIgnoreCase) ||
        goal.Contains("do it for me", StringComparison.OrdinalIgnoreCase) ||
        goal.Contains("take over", StringComparison.OrdinalIgnoreCase);
}
