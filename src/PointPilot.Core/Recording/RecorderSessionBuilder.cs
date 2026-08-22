using PointPilot.Core.Selectors;
using PointPilot.Core.Workflows;

namespace PointPilot.Core.Recording;

public sealed record RecorderControlInfo(string? AutomationId, string? Name, string? ClassName, string? ControlType);

public abstract record RecorderEvent;

/// <summary>UI Automation focus moved; null means focus left the target application.</summary>
public sealed record RecorderFocusChanged(RecorderControlInfo? Control) : RecorderEvent;

/// <summary>An invokable control in the target was activated (buttons, menu items, links).</summary>
public sealed record RecorderInvoked(RecorderControlInfo Control) : RecorderEvent;

/// <summary>A key transition observed on the keyboard while the target had foreground.</summary>
public sealed record RecorderKeyDown(string KeyName, bool Ctrl, bool Alt, bool Win, char? Character) : RecorderEvent;

/// <summary>
/// Converts an ordered stream of recorded observations into draft workflow steps.
/// Pure and deterministic so it can be unit-tested without live UIA or hooks.
/// Draft selectors are marked weak whenever they lack both an automation ID and an
/// accessible name — replay quality depends on review of those steps.
/// </summary>
public sealed class RecorderSessionBuilder
{
    private readonly List<StepSpec> _steps = [];
    private readonly List<char> _textBuffer = [];
    private RecorderControlInfo? _focusedControl;
    private readonly HashSet<string> _usedNames = new(StringComparer.Ordinal);

    public IReadOnlyList<StepSpec> Steps => _steps;

    public void Add(RecorderEvent recordedEvent)
    {
        switch (recordedEvent)
        {
            case RecorderFocusChanged focus:
                FlushText();
                _focusedControl = focus.Control;
                break;
            case RecorderInvoked invoked:
                FlushText();
                var clickName = UniqueStepName($"Click {Describe(invoked.Control)}");
                _steps.Add(new ClickStep(BuildSelector(invoked.Control), ClickKind.Single, clickName));
                break;
            case RecorderKeyDown key:
                if (key.Ctrl || key.Alt || key.Win)
                {
                    FlushText();
                    var modifiers = new List<string>();
                    if (key.Ctrl) modifiers.Add("CTRL");
                    if (key.Alt) modifiers.Add("ALT");
                    if (key.Win) modifiers.Add("WIN");
                    modifiers.Add(key.KeyName);
                    _steps.Add(new PressStep([.. modifiers], UniqueStepName($"Press {string.Join("+", modifiers)}")));
                }
                else if (key.Character is { } ch && !char.IsControl(ch))
                {
                    _textBuffer.Add(ch);
                }
                else if (IsEnterOrTab(key.KeyName))
                {
                    FlushText();
                }
                break;
        }
    }

    /// <summary>Returns the accumulated steps and clears internal buffers.</summary>
    public IReadOnlyList<StepSpec> Finish()
    {
        FlushText();
        return _steps;
    }

    private void FlushText()
    {
        if (_textBuffer.Count == 0) return;
        var text = new string([.. _textBuffer]);
        _textBuffer.Clear();
        var selector = _focusedControl is null ? null : BuildSelector(_focusedControl);
        var label = _focusedControl is null ? "focused control" : Describe(_focusedControl);
        _steps.Add(new TypeTextStep(text, selector, UniqueStepName($"Type into {label}")));
    }

    private static bool IsEnterOrTab(string keyName) => keyName is "ENTER" or "TAB";

    public static SelectorSpec BuildSelector(RecorderControlInfo control)
    {
        // Coordinates are never recorded: a recorder that emits coordinates would silently
        // produce brittle replays. When only weak criteria exist, the weakness is explicit.
        if (!string.IsNullOrWhiteSpace(control.AutomationId))
            return new SelectorSpec(AutomationId: control.AutomationId.Trim());
        if (!string.IsNullOrWhiteSpace(control.Name))
            return new SelectorSpec(Name: control.Name.Trim(), Role: control.ControlType);
        return new SelectorSpec(ClassName: control.ClassName, Role: control.ControlType);
    }

    public static bool IsWeak(RecorderControlInfo control) =>
        string.IsNullOrWhiteSpace(control.AutomationId) && string.IsNullOrWhiteSpace(control.Name);

    private static string Describe(RecorderControlInfo control) =>
        control.Name ?? control.AutomationId ?? control.ControlType ?? "control";

    private string UniqueStepName(string baseName)
    {
        var candidate = baseName;
        var suffix = 2;
        while (!_usedNames.Add(candidate))
            candidate = $"{baseName} ({suffix++})";
        return candidate;
    }
}
