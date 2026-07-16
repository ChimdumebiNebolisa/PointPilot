using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using PointPilot.Core;
using PointPilot.Infrastructure;
using PointPilot.Infrastructure.OpenAI;
using PointPilot.Infrastructure.Windows;
using Forms = System.Windows.Forms;

namespace PointPilot.App;

public partial class MainWindow : Window
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(90) };
    private readonly PointPilotStateMachine _state = new();
    private readonly TaskCoordinator _tasks = new();
    private readonly WindowContextService _windows = new();
    private readonly OverlayWindow _overlay = new();
    private readonly DevelopmentLog _log = new();
    private readonly Forms.NotifyIcon _tray;
    private readonly TaskCompletionSource _webReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private RealtimeTokenService? _tokens;
    private PointPilotWorkflow? _workflow;
    private WindowsInputExecutor? _executor;
    private GlobalHotkeyService? _hotkeys;
    private ForegroundWindowTracker? _foregroundTracker;
    private CancellationTokenSource? _activeTool;
    private bool _sessionActive;
    private bool _muted;
    private bool _allowClose;
    private bool _interruptedTask;
    private string? _pendingCallId;
    private string? _pendingAction;
    private string? _pendingPath;

    public MainWindow()
    {
        InitializeComponent();
        _state.Changed += (_, state) => { _log.Write("state_changed", new { state }); Dispatcher.InvokeAsync(() => RenderState(state)); };
        _state.Rejected += (_, transition) => _log.Write("state_transition_rejected", new { transition.From, transition.To });
        _tray = BuildTrayIcon();
        ConfigureServices();
        Loaded += async (_, _) =>
        {
            PositionCompanion();
            await InitializeRealtimeSurfaceAsync();
        };
    }

    private void ConfigureServices()
    {
        try
        {
            var options = OpenAiOptions.Load();
            _tokens = new RealtimeTokenService(_http, options);
            var visual = new OpenAiVisualReasoningService(_http, options);
            _executor = new WindowsInputExecutor(_tasks);
            var computer = new ComputerUseService(_http, options, _windows, _executor, _tasks);
            var verification = new VerificationService(visual);
            _workflow = new PointPilotWorkflow(_state, _tasks, _windows, visual, computer, verification);
        }
        catch (Exception)
        {
            ShowSafeError(ErrorMapper.Map(IntegrationFailure.MissingApiKey));
        }
    }

    private async Task InitializeRealtimeSurfaceAsync()
    {
        try
        {
            await RealtimeWebView.EnsureCoreWebView2Async();
            var core = RealtimeWebView.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultScriptDialogsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.PermissionRequested += (_, args) =>
            {
                args.State = args.PermissionKind == CoreWebView2PermissionKind.Microphone && _sessionActive
                    ? CoreWebView2PermissionState.Allow
                    : CoreWebView2PermissionState.Deny;
            };
            core.WebMessageReceived += RealtimeWebMessageReceived;
            var webRoot = Path.Combine(AppContext.BaseDirectory, "web", "dist");
            if (!Directory.Exists(webRoot)) throw new DirectoryNotFoundException("The Realtime web client was not included in this build.");
            core.SetVirtualHostNameToFolderMapping("pointpilot.local", webRoot, CoreWebView2HostResourceAccessKind.DenyCors);
            RealtimeWebView.Source = new Uri("https://pointpilot.local/index.html");
        }
        catch
        {
            ShowSafeError(ErrorMapper.Map(IntegrationFailure.Realtime));
        }
    }

    private async void SessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_state.Current == PointPilotState.Paused)
        {
            if (_tasks.Snapshot.TaskId is not null) _tasks.Resume();
            _state.Transition(PointPilotState.Listening);
            PostWeb(new { type = "mute", muted = _muted });
            return;
        }
        if (_sessionActive) { ShowWithoutStealingFocus(); return; }
        await StartSessionAsync();
    }

    private async Task StartSessionAsync()
    {
        if (_tokens is null)
        {
            ShowSafeError(ErrorMapper.Map(IntegrationFailure.MissingApiKey));
            return;
        }
        try
        {
            ClearError();
            if (_state.Current == PointPilotState.Error) _state.Transition(PointPilotState.Idle);
            _state.Transition(PointPilotState.Connecting);
            SessionButton.IsEnabled = false;
            _sessionActive = true;
            _hotkeys?.SetEscapeEnabled(true);
            await _webReady.Task.WaitAsync(TimeSpan.FromSeconds(15));
            var response = await _tokens.CreateClientSecretAsync(CancellationToken.None);
            using var document = JsonDocument.Parse(response);
            var secret = document.RootElement.GetProperty("value").GetString();
            if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Realtime client secret response was incomplete.");
            PostWeb(new { type = "connect", clientSecret = secret });
            TranscriptText.Text = "Listening… ask a follow-up whenever you’re ready.";
        }
        catch (OpenAiIntegrationException exception)
        {
            ResetSessionAfterFailure();
            ShowSafeError(exception.SafeError);
        }
        catch
        {
            ResetSessionAfterFailure();
            ShowSafeError(ErrorMapper.Map(IntegrationFailure.Realtime));
        }
    }

    private void RealtimeWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var message = JsonDocument.Parse(e.WebMessageAsJson);
            var root = message.RootElement;
            switch (root.GetProperty("type").GetString())
            {
                case "ready": _webReady.TrySetResult(); break;
                case "connected":
                    if (_state.Current == PointPilotState.Connecting) _state.Transition(PointPilotState.Listening);
                    EnableSessionControls();
                    break;
                case "disconnected": EndSession(); break;
                case "speech_started": HandleSpeechStarted(); break;
                case "transcript": RenderTranscript(root); break;
                case "tool_call": _ = HandleToolCallAsync(root.Clone()); break;
                case "error": ShowSafeError(ErrorMapper.Map(IntegrationFailure.Realtime)); break;
            }
        }
        catch
        {
            ShowSafeError(ErrorMapper.Map(IntegrationFailure.Realtime));
        }
    }

    private void HandleSpeechStarted()
    {
        var wasActing = _state.Current is PointPilotState.Planning or PointPilotState.Acting or PointPilotState.Verifying;
        if (wasActing && _tasks.Snapshot.TaskId is not null)
        {
            _tasks.Interrupt("User spoke a correction; stop before the next atomic action and re-evaluate the live screen.");
            _interruptedTask = true;
        }
        _activeTool?.Cancel();
        _overlay.Hide();
        if (_state.CanTransition(PointPilotState.Listening)) _state.Transition(PointPilotState.Listening);
        TranscriptText.Text = wasActing ? "Interrupted safely. Listening for your correction…" : "Listening…";
    }

    private async Task HandleToolCallAsync(JsonElement message)
    {
        var callId = message.GetProperty("callId").GetString() ?? string.Empty;
        var name = message.GetProperty("name").GetString() ?? string.Empty;
        _log.Write("realtime_tool_call", new { name });
        var arguments = message.GetProperty("arguments").GetString() ?? "{}";
        _activeTool?.Cancel();
        _activeTool?.Dispose();
        _activeTool = new CancellationTokenSource();
        try
        {
            if (_workflow is null) throw new InvalidOperationException("PointPilot is not configured.");
            if (_foregroundTracker?.RestoreIfPointPilotIsForeground() == false)
                throw new InvalidOperationException("Return the target application to the foreground before continuing.");
            await Task.Delay(100, _activeTool.Token);
            using var args = JsonDocument.Parse(arguments);
            WorkflowOutcome outcome = name switch
            {
                "teach" => await _workflow.TeachAsync(ReadString(args.RootElement, "request"), _activeTool.Token),
                "guide" => await _workflow.GuideAsync(ReadString(args.RootElement, "goal"), ReadString(args.RootElement, "expected_change"), _activeTool.Token),
                "act" => await HandleActAsync(args.RootElement, _activeTool.Token),
                "undo" => await _workflow.UndoAsync(_activeTool.Token),
                _ => throw new NotSupportedException($"Unsupported Realtime tool: {name}.")
            };
            ShowOutcome(outcome);
            if (outcome.RequiresConfirmation)
            {
                _pendingCallId = callId;
                _pendingAction = outcome.ConfirmationAction;
                _pendingPath = outcome.TargetPath;
                ShowConfirmation(outcome);
                return;
            }
            SendToolResult(callId, outcome.Summary);
            _interruptedTask = false;
        }
        catch (OperationCanceledException)
        {
            if (_interruptedTask) SendInterruptedToolResult(callId, "The old task revision was interrupted before its next action. Preserve completed safe steps and use the user’s correction.");
            else SendToolResult(callId, "The task was stopped before the next action. Inspect the foreground GIMP window before resuming.");
        }
        catch (OpenAiIntegrationException exception)
        {
            ShowSafeError(exception.SafeError);
            SendToolResult(callId, exception.SafeError.WhatFailed + " " + exception.SafeError.SafeNextStep);
        }
        catch (Exception exception)
        {
            var failure = exception is UnauthorizedAccessException ? IntegrationFailure.WindowChanged : IntegrationFailure.Unknown;
            var safe = ErrorMapper.Map(failure);
            ShowSafeError(safe);
            SendToolResult(callId, safe.WhatFailed + " " + safe.SafeNextStep);
        }
    }

    private Task<WorkflowOutcome> HandleActAsync(JsonElement args, CancellationToken cancellationToken)
    {
        var goal = ReadString(args, "goal");
        var path = ReadString(args, "export_path");
        var constraints = args.TryGetProperty("constraints", out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray()
            : [];
        if (_interruptedTask)
            return _workflow!.ReviseActAsync(goal, constraints, path, cancellationToken);
        return _workflow!.ActAsync(goal, constraints, path, cancellationToken);
    }

    private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (_workflow is null || _pendingCallId is null || _pendingAction is null) return;
        try
        {
            ConfirmationPanel.Visibility = Visibility.Collapsed;
            if (_foregroundTracker?.RestoreIfPointPilotIsForeground() == false)
                throw new InvalidOperationException("Return GIMP to the foreground before confirming.");
            await Task.Delay(100);
            var outcome = await _workflow.ConfirmAndExecuteAsync(_pendingAction, _pendingPath, CancellationToken.None);
            ShowOutcome(outcome);
            SendToolResult(_pendingCallId, outcome.Summary);
            ClearPendingConfirmation();
        }
        catch (Exception exception)
        {
            var safe = exception is OpenAiIntegrationException integration ? integration.SafeError : ErrorMapper.Map(IntegrationFailure.Unknown);
            ShowSafeError(safe);
            SendToolResult(_pendingCallId, safe.WhatFailed + " " + safe.SafeNextStep);
            ClearPendingConfirmation();
        }
    }

    private void CancelConfirmation_Click(object sender, RoutedEventArgs e)
    {
        if (_tasks.Snapshot.TaskId is not null) _tasks.Pause();
        if (_state.CanTransition(PointPilotState.Paused)) _state.Transition(PointPilotState.Paused);
        if (_state.Current == PointPilotState.Paused) _state.Transition(PointPilotState.Listening);
        if (_pendingCallId is not null) SendToolResult(_pendingCallId, "The user declined the exact export. No export was authorized.");
        ClearPendingConfirmation();
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        _muted = !_muted;
        MuteButton.Content = _muted ? "_Unmute" : "_Mute";
        PostWeb(new { type = "mute", muted = _muted });
        TranscriptText.Text = _muted ? "Microphone muted." : "Listening…";
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e) => PauseCurrentTask();
    private void EndButton_Click(object sender, RoutedEventArgs e) => EndSession();

    private void PauseCurrentTask()
    {
        _activeTool?.Cancel();
        if (_tasks.Snapshot.TaskId is not null) _tasks.Pause();
        PostWeb(new { type = "cancel_response" });
        if (_pendingCallId is not null)
            SendInterruptedToolResult(_pendingCallId, "The user stopped the pending operation. Its confirmation is invalidated.");
        ClearPendingConfirmation();
        _overlay.Hide();
        if (_state.CanTransition(PointPilotState.Paused)) _state.Transition(PointPilotState.Paused);
        TranscriptText.Text = "Paused. No further computer action will run until you resume.";
    }

    private void EndSession()
    {
        _activeTool?.Cancel();
        if (_tasks.Snapshot.TaskId is not null) _tasks.Pause();
        if (_sessionActive) PostWeb(new { type = "disconnect" });
        _sessionActive = false;
        _muted = false;
        _interruptedTask = false;
        _overlay.Hide();
        _hotkeys?.SetEscapeEnabled(false);
        if (_state.Current != PointPilotState.Idle)
        {
            if (_state.CanTransition(PointPilotState.Idle)) _state.Transition(PointPilotState.Idle);
            else if (_state.CanTransition(PointPilotState.Paused)) { _state.Transition(PointPilotState.Paused); _state.Transition(PointPilotState.Idle); }
        }
        SessionButton.IsEnabled = true;
        SessionButton.Content = "_Start listening";
        MuteButton.IsEnabled = PauseButton.IsEnabled = EndButton.IsEnabled = false;
        TranscriptText.Text = "Session ended. Start again when you want PointPilot nearby.";
        ClearPendingConfirmation();
    }

    private void ShowOutcome(WorkflowOutcome outcome)
    {
        TranscriptText.Text = outcome.Summary;
        if (outcome.Snapshot is not null)
        {
            ContextTitle.Text = string.IsNullOrWhiteSpace(outcome.Snapshot.Title) ? outcome.Snapshot.ProcessName : outcome.Snapshot.Title;
            _overlay.ShowTarget(outcome.Snapshot, outcome.Target);
        }
    }

    private void ShowConfirmation(WorkflowOutcome outcome)
    {
        ConfirmationDetails.Text = $"{outcome.ConfirmationAction}\nTarget: {outcome.TargetPath}\nThis confirms only this task revision and exact path. Existing data may be replaced.";
        ConfirmationPanel.Visibility = Visibility.Visible;
        ConfirmButton.Focus();
    }

    private void ClearPendingConfirmation()
    {
        _pendingCallId = _pendingAction = _pendingPath = null;
        ConfirmationPanel.Visibility = Visibility.Collapsed;
    }

    private void RenderTranscript(JsonElement root)
    {
        var text = root.TryGetProperty("text", out var value) ? value.GetString() : null;
        if (!string.IsNullOrWhiteSpace(text)) TranscriptText.Text = text;
    }

    private void RenderState(PointPilotState state)
    {
        StateText.Text = state.ToString();
        StateDot.Fill = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(state switch
        {
            PointPilotState.Listening => "#12B76A",
            PointPilotState.Acting or PointPilotState.Planning or PointPilotState.Verifying => "#2563EB",
            PointPilotState.Error => "#D92D20",
            PointPilotState.Paused => "#F79009",
            _ => "#98A2B3"
        }));
        SessionButton.Content = state == PointPilotState.Paused ? "_Resume listening" : _sessionActive ? "_Listening" : "_Start listening";
        SessionButton.IsEnabled = state is PointPilotState.Idle or PointPilotState.Paused or PointPilotState.Error;
    }

    private void EnableSessionControls()
    {
        MuteButton.IsEnabled = PauseButton.IsEnabled = EndButton.IsEnabled = true;
        SessionButton.IsEnabled = false;
    }

    private void ShowSafeError(SafeError error)
    {
        _log.Write("safe_error", new { error.Failure, error.ActionMayHaveOccurred });
        Dispatcher.InvokeAsync(() =>
        {
            ErrorText.Text = $"{error.WhatFailed} {error.SafeNextStep}";
            ErrorText.Visibility = Visibility.Visible;
            TranscriptText.Text = $"{error.WhatFailed}\n{error.UserInspection}\n{error.SafeNextStep}";
            if (_state.Current != PointPilotState.Error && _state.CanTransition(PointPilotState.Error)) _state.Transition(PointPilotState.Error);
        });
    }

    private void ClearError() => ErrorText.Visibility = Visibility.Collapsed;
    private static string ReadString(JsonElement root, string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private void SendToolResult(string callId, string output) => PostWeb(new { type = "tool_result", callId, output = SecretRedactor.Redact(output) });
    private void SendInterruptedToolResult(string callId, string output) => PostWeb(new { type = "tool_interrupted", callId, output = SecretRedactor.Redact(output) });
    private void PostWeb(object value)
    {
        if (RealtimeWebView.CoreWebView2 is null) return;
        RealtimeWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(value));
    }

    private void ResetSessionAfterFailure()
    {
        _sessionActive = false;
        _hotkeys?.SetEscapeEnabled(false);
        SessionButton.IsEnabled = true;
    }

    private Forms.NotifyIcon BuildTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open PointPilot", null, (_, _) => Dispatcher.Invoke(ShowWithoutStealingFocus));
        menu.Items.Add("Start listening", null, (_, _) => Dispatcher.Invoke(async () => await StartSessionAsync()));
        menu.Items.Add("End session", null, (_, _) => Dispatcher.Invoke(EndSession));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => Dispatcher.Invoke(Quit));
        var tray = new Forms.NotifyIcon
        {
            Text = "PointPilot — voice-first desktop companion",
            Icon = System.Drawing.SystemIcons.Information,
            Visible = true,
            ContextMenuStrip = menu
        };
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowWithoutStealingFocus);
        return tray;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            _foregroundTracker = new ForegroundWindowTracker();
            _hotkeys = new GlobalHotkeyService(new WindowInteropHelper(this).Handle);
            _hotkeys.ActivateRequested += async (_, _) =>
            {
                ShowWithoutStealingFocus();
                if (!_sessionActive) await StartSessionAsync();
            };
            _hotkeys.StopRequested += (_, _) => PauseCurrentTask();
        }
        catch (Exception)
        {
            ErrorText.Text = "The default global hotkey is unavailable. Use the tray icon or Start listening button.";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void PositionCompanion()
    {
        Left = SystemParameters.WorkArea.Right - ActualWidth - 18;
        Top = SystemParameters.WorkArea.Bottom - ActualHeight - 18;
    }

    private void ShowWithoutStealingFocus()
    {
        if (!IsVisible) Show();
        PositionCompanion();
        ShowWindow(new WindowInteropHelper(this).Handle, 4);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        Hide();
    }

    private void Quit()
    {
        _allowClose = true;
        EndSession();
        _hotkeys?.Dispose();
        _foregroundTracker?.Dispose();
        _executor?.Dispose();
        _tasks.Dispose();
        _activeTool?.Dispose();
        _http.Dispose();
        _overlay.Close();
        _tray.Visible = false;
        _tray.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool ShowWindow(nint hWnd, int command);
}
