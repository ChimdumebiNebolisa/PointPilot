using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using PointPilot.Core.Engine;
using PointPilot.Core.Tracing;
using PointPilot.Core.Workflows;
using PointPilot.Infrastructure;
using PointPilot.Infrastructure.Recording;
using PointPilot.Infrastructure.Verification;
using PointPilot.Infrastructure.Windows;
using Forms = System.Windows.Forms;

namespace PointPilot.App;

public partial class MainWindow : Window
{
    private readonly DevelopmentLog _log = new();
    private readonly OverlayWindow _overlay = new();
    private readonly Forms.NotifyIcon _tray;
    private readonly WorkflowRunner _runner;
    private readonly UiElementCatalog _catalog = new();
    private GlobalHotkeyService? _hotkeys;
    private CancellationTokenSource? _activeRun;
    private WorkflowDefinition? _workflow;
    private WorkflowDefinition? _lastDraft;
    private UiAutomationRecorder? _recorder;
    private bool _allowClose;
    private string? _traceDirectory;

    public MainWindow()
    {
        InitializeComponent();
        _tray = BuildTrayIcon();
        _runner = CreateRunner();
        Loaded += (_, _) => RefreshWindows();
    }

    private static WorkflowRunner CreateRunner() => new(
        new WindowBinder(),
        new WindowsInputExecutor(),
        new ForegroundMonitor(),
        new ScreenCaptureService(),
        new ExactImageComparer(),
        new SystemClock());

    // ---- Target selection ---------------------------------------------------------------

    private void RefreshWindows_Click(object sender, RoutedEventArgs e) => RefreshWindows();

    private void RefreshWindows()
    {
        var previous = (TargetCombo.SelectedItem as TopLevelWindowInfo)?.Handle;
        var windows = _catalog.ListTopLevelWindows();
        TargetCombo.ItemsSource = windows;
        TargetCombo.DisplayMemberPath = nameof(TopLevelWindowInfo.DisplayName);
        if (previous is { } handle)
        {
            var match = windows.FirstOrDefault(w => w.Handle == handle);
            if (match is not null) TargetCombo.SelectedItem = match;
        }
        _log.Write("windows_listed", new { count = windows.Count });
    }

    private void Target_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        TargetDetails.Text = TargetCombo.SelectedItem is TopLevelWindowInfo target
            ? $"{target.ProcessName}.exe — pid {target.ProcessId}, hwnd 0x{target.Handle:x}, '{target.Title}'"
            : "Select the running application a workflow should bind to.";

    // ---- Workflow load / record ---------------------------------------------------------

    private void LoadWorkflow_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "PointPilot workflow (*.yaml;*.yml)|*.yaml;*.yml|All files (*.*)|*.*", Title = "Load workflow" };
        if (dialog.ShowDialog(this) != true) return;
        LoadWorkflowFile(dialog.FileName);
    }

    private void LoadWorkflowFile(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            ApplyParsed(WorkflowParser.Parse(text, path));
            WorkflowPathText.Text = $"Loaded: {path}";
        }
        catch (IOException ex)
        {
            ShowValidation($"The workflow file could not be read: {ex.Message}");
        }
    }

    private void ApplyParsed(WorkflowParseResult parsed)
    {
        if (!parsed.Success)
        {
            _workflow = null;
            StepsList.ItemsSource = null;
            ShowValidation(string.Join(Environment.NewLine, parsed.Diagnostics.Select(d => $"• {d.Path}: {d.Message}")));
            return;
        }
        ClearValidation();
        _workflow = parsed.Definition;
        StepsList.ItemsSource = parsed.Definition!.Steps.Select((s, i) =>
        {
            var selector = WorkflowRunner.SelectorOf(s);
            var weak = selector is not null && WorkflowRunner.IsWeakSelector(selector);
            return $"{i + 1}. {WorkflowRunner.KindOf(s)}{(s.Name is null ? "" : $" — {s.Name}")}{(weak ? "   [weak target]" : "")}";
        }).ToList();
        StatusText.Text = $"Workflow '{parsed.Definition.Name}' validated with {parsed.Definition.Steps.Count} steps.";
        UpdateRunButtons();
    }

    private async void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_recorder is not null) { StopRecording(); return; }
        if (TargetCombo.SelectedItem is not TopLevelWindowInfo target)
        {
            ShowValidation("Select a target window before recording.");
            return;
        }
        try
        {
            _recorder = UiAutomationRecorder.Start(new Core.Workflows.TargetSpec(target.ProcessName, ProcessMatchMode.Exact, null));
            RecordButton.Content = "S_top recording";
            StatusText.Text = $"Recording interactions with {target.ProcessName}. Click controls and type as usual, then stop recording.";
            SaveDraftButton.IsEnabled = false;
        }
        catch (Core.Elements.StepFailureException ex)
        {
            ShowValidation(ex.Message);
        }
        await Task.CompletedTask;
    }

    private void StopRecording()
    {
        if (_recorder is null) return;
        _lastDraft = _recorder.Stop();
        _recorder.Dispose();
        _recorder = null;
        RecordButton.Content = "_Start recording";
        SaveDraftButton.IsEnabled = true;
        ApplyParsed(WorkflowParser.Parse(WorkflowYamlWriter.Write(_lastDraft), "(recorded draft)"));
        _workflow = _lastDraft;
        StatusText.Text = "Recording stopped. The draft is loaded below — review weak selectors, then save or run it.";
    }

    private void SaveDraft_Click(object sender, RoutedEventArgs e)
    {
        if (_workflow is null) return;
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "PointPilot workflow (*.yaml)|*.yaml", FileName = $"{_workflow.Name}.yaml", Title = "Save workflow draft" };
        if (dialog.ShowDialog(this) != true) return;
        File.WriteAllText(dialog.FileName, WorkflowYamlWriter.Write(_workflow));
        WorkflowPathText.Text = $"Saved: {dialog.FileName}";
    }

    // ---- Run lifecycle -------------------------------------------------------------------

    private async void DryRun_Click(object sender, RoutedEventArgs e) => await ExecuteAsync(dryRun: true);

    private async void Run_Click(object sender, RoutedEventArgs e) => await ExecuteAsync(dryRun: false);

    private async Task ExecuteAsync(bool dryRun)
    {
        if (_workflow is null)
        {
            ShowValidation("Load or record a workflow first.");
            return;
        }
        if (_activeRun is not null) return;
        SetState(RunState.Running);
        _activeRun = new CancellationTokenSource();
        StopButton.IsEnabled = true;
        LoadButton.IsEnabled = RecordButton.IsEnabled = DryRunButton.IsEnabled = RunButton.IsEnabled = false;
        ResultBox.Text = dryRun ? "Dry run in progress…" : "Run in progress…";
        try
        {
            var outputDirectory = Path.Combine(AppContext.BaseDirectory, "traces", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            _traceDirectory = outputDirectory;
            var result = await _runner.ExecuteAsync(
                _workflow,
                new RunOptions(new Dictionary<string, string>(), dryRun, outputDirectory, MachineInfoBuilder.Build()),
                _activeRun.Token);
            ResultBox.Text = result.Summary;
            StatusText.Text = $"Run {result.Trace.Status}. Trace artifacts: {outputDirectory}";
            _overlay.FlashResolved(result.Trace);
            _log.Write("run_completed", new { status = result.Trace.Status, steps = result.Trace.Steps.Count });
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Run cancelled. No further actions were sent.";
            ResultBox.Text += Environment.NewLine + "Cancelled at the next atomic action boundary.";
        }
        finally
        {
            _activeRun.Dispose();
            _activeRun = null;
            StopButton.IsEnabled = false;
            LoadButton.IsEnabled = RecordButton.IsEnabled = DryRunButton.IsEnabled = RunButton.IsEnabled = true;
            SetState(RunState.Idle);
            UpdateRunButtons();
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        if (_recorder is not null) { StopRecording(); return; }
        _activeRun?.Cancel();
    }

    private void OpenTrace_Click(object sender, RoutedEventArgs e)
    {
        if (_traceDirectory is null || !Directory.Exists(_traceDirectory)) return;
        using var process = Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_traceDirectory}\"") { UseShellExecute = true });
    }

    private void UpdateRunButtons() =>
        DryRunButton.IsEnabled = RunButton.IsEnabled = _workflow is not null && _activeRun is null;

    // ---- Diagnostics and shell ------------------------------------------------------------

    private void ShowValidation(string message)
    {
        ValidationText.Text = message;
        ValidationText.Visibility = Visibility.Visible;
        StatusText.Text = "The workflow needs attention before it can run.";
        _log.Write("validation_failed", new { length = message.Length });
    }

    private void ClearValidation()
    {
        ValidationText.Visibility = Visibility.Collapsed;
        ValidationText.Text = "";
    }

    private void SetState(RunState state)
    {
        StateText.Text = state.ToString();
        StateDot.Fill = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(state switch
            {
                RunState.Running => "#2563EB",
                RunState.Completed => "#12B76A",
                RunState.Failed => "#D92D20",
                RunState.Cancelled => "#F79009",
                _ => "#98A2B3"
            }));
    }

    private Forms.NotifyIcon BuildTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open PointPilot", null, (_, _) => Dispatcher.Invoke(ShowWithoutStealingFocus));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => Dispatcher.Invoke(Quit));
        var tray = new Forms.NotifyIcon
        {
            Text = "PointPilot — deterministic workflow runner",
            Icon = System.Drawing.SystemIcons.Shield,
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
            _hotkeys = new GlobalHotkeyService(new WindowInteropHelper(this).Handle);
            _hotkeys.StopRequested += (_, _) => Dispatcher.Invoke(() => { if (_activeRun is not null) _activeRun.Cancel(); });
            _hotkeys.ActivateRequested += (_, _) => Dispatcher.Invoke(ShowWithoutStealingFocus);
        }
        catch
        {
            StatusText.Text = "Global hotkeys are unavailable (another app owns Ctrl+Alt+Space). The Stop button still cancels runs.";
        }
    }

    private void ShowWithoutStealingFocus()
    {
        if (!IsVisible) Show();
        Activate();
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
        _activeRun?.Cancel();
        _recorder?.Dispose();
        _hotkeys?.Dispose();
        _overlay.Close();
        _tray.Visible = false;
        _tray.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }
}
