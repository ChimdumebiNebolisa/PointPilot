using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using PointPilot.Core;
using PointPilot.Core.Engine;
using PointPilot.Core.Recording;using PointPilot.Infrastructure.Windows;
using PointPilot.Core.Workflows;
using StepFailureException = PointPilot.Core.Elements.StepFailureException;

namespace PointPilot.Infrastructure.Recording;

/// <summary>
/// Event-driven recorder: observes UIA focus and invoke events plus foreground-filtered
/// keyboard input while the user works in the bound window. The recorder never sends
/// input. Output is a draft workflow whose weak selectors are flagged for review.
/// </summary>
public sealed class UiAutomationRecorder : IDisposable
{
    private readonly RecorderSessionBuilder _builder = new();
    private readonly string _processName;
    private readonly nint _windowHandle;
    private readonly uint _pid;
    private readonly System.Windows.Automation.AutomationElement _scopeRoot;
    private System.Windows.Automation.AutomationEventHandler? _invokeHandler;
    private System.Windows.Automation.AutomationFocusChangedEventHandler? _focusHandler;
    private KeyboardHook? _keyboardHook;
    private bool _running;

    private UiAutomationRecorder(UiaSession session)
    {
        _processName = session.ProcessName;
        _windowHandle = session.WindowHandle;
        _pid = session.ProcessId;
        _scopeRoot = session.RootAutomationElement;
    }

    public static UiAutomationRecorder Start(TargetSpec target)
    {
        if (target.WindowTitleRegex is { } pattern)
            throw new StepFailureException("Recording requires a target without a windowTitleRegex constraint; bind by process name instead.");
        var binder = new WindowBinder();
        UiaSession session;
        try { session = (UiaSession)binder.Bind(target, CancellationToken.None); }
        catch (StepFailureException) { throw; }
        var recorder = new UiAutomationRecorder(session);
        recorder.StartCore(target);
        return recorder;
    }

    private void StartCore(TargetSpec target)
    {
        _invokeHandler = OnInvoke;
        System.Windows.Automation.Automation.AddAutomationEventHandler(
            System.Windows.Automation.InvokePatternIdentifiers.InvokedEvent,
            _scopeRoot,
            System.Windows.Automation.TreeScope.Subtree,
            _invokeHandler);

        _focusHandler = OnFocusChanged;
        System.Windows.Automation.Automation.AddAutomationFocusChangedEventHandler(_focusHandler);

        _keyboardHook = new KeyboardHook(key =>
        {
            if (NativeMethods.GetForegroundWindow() != _windowHandle) return;
            var (ctrl, alt, win) = GetModifierState();
            _builder.Add(new RecorderKeyDown(NameVirtualKey(key, ctrl), ctrl, alt, win, ToChar(key)));
        });

        _running = true;
    }

    /// <summary>Stops observation and returns a draft workflow targeting the recorded process.</summary>
    public WorkflowDefinition Stop()
    {
        if (!_running) throw new InvalidOperationException("The recorder is not running.");
        Unregister();
        var steps = new List<Core.Workflows.StepSpec> { new Core.Workflows.FocusWindowStep("Bring recorded window to foreground") };
        steps.AddRange(_builder.Finish());

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var definition = new WorkflowDefinition(
            SchemaVersion: 1,
            Name: $"recorded-{_processName}-{stamp}",
            Description: "Recorded draft. Review selectors before relying on replay; weak targets are expected to need refinement.",
            Variables: [],
            Target: new TargetSpec(_processName, ProcessMatchMode.Exact, null),
            Defaults: new DefaultsSpec(5000),
            Steps: [.. steps],
            SourcePath: "(recorded)",
            SourceHash: "");
        var yaml = WorkflowYamlWriter.Write(definition);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(yaml))).ToLowerInvariant();
        return definition with { SourceHash = hash };
    }

    private void OnInvoke(object? sender, System.Windows.Automation.AutomationEventArgs e)
    {
        try
        {
            if (sender is not System.Windows.Automation.AutomationElement element) return;
            var info = DescribeElement(element);
            if (info is not null) _builder.Add(new RecorderInvoked(info));
        }
        catch (System.Windows.Automation.ElementNotAvailableException) { /* raced with teardown */ }
    }

    private void OnFocusChanged(object? sender, System.Windows.Automation.AutomationFocusChangedEventArgs e)
    {
        try
        {
            var focused = System.Windows.Automation.AutomationElement.FocusedElement;
            if (focused is null || !BelongsToTarget(focused))
            {
                _builder.Add(new RecorderFocusChanged(null));
                return;
            }
            var info = DescribeElement(focused);
            _builder.Add(new RecorderFocusChanged(info));
        }
        catch (System.Windows.Automation.ElementNotAvailableException)
        {
            _builder.Add(new RecorderFocusChanged(null));
        }
    }

    private bool BelongsToTarget(System.Windows.Automation.AutomationElement element)
    {
        var handle = new nint(element.Current.NativeWindowHandle);
        if (handle != 0)
        {
            _ = NativeMethods.GetWindowThreadProcessId(handle, out var windowPid);
            if (windowPid != _pid) return false;
            return true;
        }
        // Non-top-level elements report hwnd 0; walk ancestors to find the owning window.
        var walker = System.Windows.Automation.TreeWalker.ControlViewWalker;
        var current = element;
        for (var depth = 0; depth < 12 && current is not null; depth++)
        {
            current = walker.GetParent(current);
            if (current == System.Windows.Automation.AutomationElement.RootElement) break;
            var ancestorHandle = new nint(current.Current.NativeWindowHandle);
            if (ancestorHandle != 0)
            {
                _ = NativeMethods.GetWindowThreadProcessId(ancestorHandle, out var ancestorPid);
                return ancestorPid == _pid;
            }
        }
        return false;
    }

    private static RecorderControlInfo? DescribeElement(System.Windows.Automation.AutomationElement element)
    {
        try
        {
            var current = element.Current;
            return new RecorderControlInfo(current.AutomationId, current.Name, current.ClassName, current.ControlType?.ProgrammaticName?.Replace("ControlType.", "", StringComparison.Ordinal));
        }
        catch (System.Windows.Automation.ElementNotAvailableException)
        {
            return null;
        }
    }

    private static (bool Ctrl, bool Alt, bool Win) GetModifierState() =>
        (
            (NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0,
            (NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0,
            (NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0 || (NativeMethods.GetAsyncKeyState(0x5C) & 0x8000) != 0
        );

    internal static string NameVirtualKey(int virtualKey, bool ctrlHeld)
    {
        if (ctrlHeld && char.IsLetterOrDigit((char)virtualKey)) return char.ToUpperInvariant((char)virtualKey).ToString();
        return virtualKey switch
        {
            0x08 => "BACKSPACE",
            0x09 => "TAB",
            0x0D => "ENTER",
            0x1B => "ESCAPE",
            0x20 => "SPACE",
            0x21 => "PAGEUP",
            0x22 => "PAGEDOWN",
            0x23 => "END",
            0x24 => "HOME",
            >= 0x25 and <= 0x28 => ((virtualKey - 0x25) switch { 0 => "LEFT", 1 => "UP", 2 => "RIGHT", _ => "DOWN" }),
            0x2E => "DELETE",
            >= 0x70 and <= 0x87 => $"F{virtualKey - 0x70 + 1}",
            >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(),
            >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(),
            >= 0x60 and <= 0x69 => ((char)(virtualKey - 0x60 + '0')).ToString(),
            _ => virtualKey.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static char? ToChar(int virtualKey)
    {
        var ch = NativeMethods.MapVirtualKey(virtualKey, 2); // MAPVK_VK_TO_CHAR
        if (ch is < 32 or > 126) return null;
        return (char)ch;
    }

    private void Unregister()
    {
        if (!_running) return;
        _running = false;
        try
        {
            if (_invokeHandler is not null) System.Windows.Automation.Automation.RemoveAutomationEventHandler(System.Windows.Automation.InvokePatternIdentifiers.InvokedEvent, _scopeRoot, _invokeHandler);
            if (_focusHandler is not null) System.Windows.Automation.Automation.RemoveAutomationFocusChangedEventHandler(_focusHandler);
        }
        catch (System.Windows.Automation.ElementNotAvailableException) { /* scope window already gone */ }
        finally
        {
            _invokeHandler = null;
            _focusHandler = null;
            _keyboardHook?.Dispose();
            _keyboardHook = null;
        }
    }

    public void Dispose() => Unregister();
}

/// <summary>Low-level keyboard hook that must live on a thread pumping messages (the WPF dispatcher).</summary>
internal sealed class KeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private readonly NativeMethods.HookProc _proc;
    private readonly nint _hook;
    private readonly Action<int> _onKeyDown;

    public KeyboardHook(Action<int> onKeyDown)
    {
        _onKeyDown = onKeyDown;
        _proc = Proc;
        _hook = NativeMethods.SetWindowsHookExW(WhKeyboardLl, _proc, 0, 0);
        if (_hook == 0) throw new StepFailureException("The keyboard hook could not be registered; recording keyboard input requires an interactive desktop.");
    }

    private nint Proc(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && wParam == WmKeydown)
        {
            var info = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
            _onKeyDown(info.VirtualKeyCode);
        }
        return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != 0) NativeMethods.UnhookWindowsHookEx(_hook);
        GC.KeepAlive(_proc);
    }

    private delegate nint HookProc(int code, nint wParam, nint lParam);
}
