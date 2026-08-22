using System.Diagnostics;
using System.Windows.Automation;
using PointPilot.Core;
using PointPilot.Core.Elements;
using PointPilot.Core.Selectors;
using StepFailureException = PointPilot.Core.Elements.StepFailureException;

namespace PointPilot.Infrastructure.Windows;

/// <summary>
/// Live UI Automation binding to one top-level window. Every query walks the tree fresh;
/// no AutomationElement references are cached between calls, so stale-element races are
/// minimized by construction and any residual race fails closed in the executor.
/// </summary>
public sealed class UiaSession : IUiaSession
{
    private readonly AutomationElement _root;

    internal UiaSession(AutomationElement root, uint processId, string processName)
    {
        _root = root;
        ProcessId = processId;
        ProcessName = processName;
        WindowHandle = new nint(root.Current.NativeWindowHandle);
    }

    public nint WindowHandle { get; }
    public uint ProcessId { get; }
    public string ProcessName { get; }
    public string Title => SafeTitle();

    public WindowBounds LiveBounds
    {
        get
        {
            try
            {
                var rect = _root.Current.BoundingRectangle;
                if (rect.IsEmpty) return new(0, 0, 0, 0);
                return new((int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height);
            }
            catch (ElementNotAvailableException)
            {
                return new(0, 0, 0, 0);
            }
        }
    }

    public IUiElement RootElement => new UiaElement(_root);

    internal AutomationElement RootAutomationElement => _root;

    public IReadOnlyList<IUiElement> EnumerateSubtree()
    {
        var results = new List<IUiElement>();
        Walk(_root, results);
        return results;

        static void Walk(AutomationElement element, List<IUiElement> results)
        {
            results.Add(new UiaElement(element));
            try
            {
                var walker = TreeWalker.ControlViewWalker;
                for (var child = walker.GetFirstChild(element); child is not null; child = walker.GetNextSibling(child))
                    Walk(child, results);
            }
            catch (ElementNotAvailableException)
            {
                // The tree mutated mid-walk; elements collected so far remain valid for this pass.
            }
        }
    }

    private string SafeTitle()
    {
        try { return _root.Current.Name ?? string.Empty; }
        catch (ElementNotAvailableException) { return string.Empty; }
    }

    public void Dispose() { /* AutomationElement requires no explicit unmanaged release */ }

    internal static UiaSession FromHandle(nint hwnd)
    {
        if (!NativeMethods.IsWindow(hwnd)) throw new StepFailureException("The target window handle is no longer valid.");
        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        var element = AutomationElement.FromHandle(hwnd) ?? throw new StepFailureException($"UI Automation could not attach to window 0x{hwnd:x}.");
        string processName;
        using (var process = Process.GetProcessById(checked((int)processId)))
            processName = process.ProcessName;
        return new(element, processId, processName);
    }
}

internal sealed class UiaElement : IUiElement
{
    private readonly AutomationElement _element;
    private readonly WindowBounds _bounds;

    internal UiaElement(AutomationElement element)
    {
        _element = element;
        Identity = new ElementIdentity(
            Read(() => element.Current.AutomationId),
            Read(() => element.Current.Name),
            Read(() => element.Current.ClassName),
            Read(() => element.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "", StringComparison.Ordinal)));
        IsEnabled = Read(() => element.Current.IsEnabled);
        IsOffscreen = Read(() => element.Current.IsOffscreen);
        _bounds = ReadBounds();
        Value = Read(() =>
            element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern)
                ? ((ValuePattern)pattern).Current.Value
                : null);
    }

    private static T? Read<T>(Func<T> read)
    {
        try { return read(); }
        catch (ElementNotAvailableException) { return default; }
    }

    private WindowBounds ReadBounds()
    {
        try
        {
            var rect = _element.Current.BoundingRectangle;
            if (rect.IsEmpty || double.IsInfinity(rect.Width)) return new(0, 0, 0, 0);
            return new((int)rect.Left, (int)rect.Top, (int)Math.Ceiling(rect.Width), (int)Math.Ceiling(rect.Height));
        }
        catch (ElementNotAvailableException)
        {
            return new(0, 0, 0, 0);
        }
    }

    public ElementIdentity Identity { get; }
    public bool IsEnabled { get; }
    public bool IsOffscreen { get; }
    public WindowBounds Bounds => _bounds;
    public string? Value { get; }

    public void Focus()
    {
        try { _element.SetFocus(); }
        catch (ElementNotAvailableException ex)
        {
            throw new StepFailureException("The resolved element disappeared before it could be focused.", ex);
        }
    }
}
