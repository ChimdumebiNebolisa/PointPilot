using PointPilot.Core.Selectors;

namespace PointPilot.Core.Workflows;

public enum ProcessMatchMode { Exact, Prefix }
public enum ClickKind { Single, Double, Right }
public enum FileCondition { Exists, NotExists }
public enum WindowCondition { Visible, Minimized, Closed, Foreground }
public enum ControlState { Exists, Visible, Enabled, Value }

public sealed record WorkflowDefinition(
    int SchemaVersion,
    string Name,
    string? Description,
    IReadOnlyList<VariableSpec> Variables,
    TargetSpec Target,
    DefaultsSpec Defaults,
    IReadOnlyList<StepSpec> Steps,
    string SourcePath,
    string SourceHash);

public sealed record VariableSpec(string Name, bool Required, string? Default);

public sealed record TargetSpec(string ProcessName, ProcessMatchMode ProcessNameMatch, string? WindowTitleRegex);

public sealed record DefaultsSpec(int TimeoutMs);

public abstract record StepSpec(string? Name, int? TimeoutMs);

public sealed record FocusWindowStep(string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record FocusControlStep(SelectorSpec Selector, string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record ClickStep(SelectorSpec Selector, ClickKind Kind, string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record TypeTextStep(string Text, SelectorSpec? Selector, string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record PressStep(IReadOnlyList<string> Keys, string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record WaitStep(WaitCondition Condition, string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record ScreenshotStep(string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record AssertFileStep(string Path, FileCondition Condition, string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record AssertWindowStep(WindowCondition Condition, string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record AssertControlStep(SelectorSpec Selector, ControlState State, string? Value, string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public sealed record AssertImageStep(SelectorSpec Selector, string ReferenceImage, int MaxChannelDelta, string? Name = null, int? TimeoutMs = null) : StepSpec(Name, TimeoutMs);

public abstract record WaitCondition;

public sealed record WaitForWindowTitle(string Regex) : WaitCondition;

public sealed record WaitForControl(SelectorSpec Selector, ControlState State) : WaitCondition;

public sealed record WaitForFile(string Path, FileCondition Condition) : WaitCondition;

public sealed record DelayMs(int Milliseconds) : WaitCondition;
