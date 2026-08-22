using PointPilot.Core.Engine;
using PointPilot.Core.Workflows;
using PointPilot.Infrastructure;
using PointPilot.Infrastructure.Verification;
using PointPilot.Infrastructure.Windows;
using StepFailureException = PointPilot.Core.Elements.StepFailureException;

namespace PointPilot.Cli;

/// <summary>
/// Headless host for the same parser and engine the desktop app uses.
/// Exit codes: 0 completed, 2 invalid workflow, 3 run failed, 4 cancelled, 1 usage error.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0) return Usage();
        var command = args[0].ToLowerInvariant();
        try
        {
            return command switch
            {
                "validate" => Validate(Rest(args)),
                "run" => await RunAsync(Rest(args)),
                "--help" or "-h" or "help" => Usage(),
                _ => Unknown(command)
            };
        }
        catch (StepFailureException ex)
        {
            Console.Error.WriteLine("pointpilot: " + ex.Message);
            return 3;
        }
    }

    private static int Validate(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: pointpilot validate <workflow.yaml>");
            return 1;
        }
        var (text, path) = ReadWorkflow(args[0]);
        var parsed = WorkflowParser.Parse(text, path);
        if (!parsed.Success)
        {
            foreach (var diagnostic in parsed.Diagnostics)
                Console.Error.WriteLine($"invalid: {diagnostic}");
            return 2;
        }
        var definition = parsed.Definition!;
        Console.WriteLine($"valid: '{definition.Name}' ({definition.Steps.Count} steps) targeting process '{definition.Target.ProcessName}'");
        for (var i = 0; i < definition.Steps.Count; i++)
        {
            var step = definition.Steps[i];
            var selector = WorkflowRunner.SelectorOf(step);
            var weak = selector is not null && WorkflowRunner.IsWeakSelector(selector);
            Console.WriteLine($"  {i + 1}. {WorkflowRunner.KindOf(step)}{(weak ? " [weak target]" : "")}");
        }
        return 0;
    }

    private static async Task<int> RunAsync(string[] args)
    {
        string? path = null, output = null;
        bool dryRun = false;
        var variables = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--out":
                    if (++i >= args.Length) { Console.Error.WriteLine("--out requires a directory."); return 1; }
                    output = args[i];
                    break;
                case "--var":
                    if (++i >= args.Length || !args[i].Contains('=')) { Console.Error.WriteLine("--var requires key=value."); return 1; }
                    var separator = args[i].IndexOf('=');
                    variables[args[i][..separator]] = args[i][(separator + 1)..];
                    break;
                default:
                    if (path is null) path = args[i];
                    else { Console.Error.WriteLine($"Unexpected argument '{args[i]}'."); return 1; }
                    break;
            }
        }
        if (path is null)
        {
            Console.Error.WriteLine("usage: pointpilot run <workflow.yaml> [--var name=value]... [--dry-run] [--out <dir>]");
            return 1;
        }

        var (text, resolvedPath) = ReadWorkflow(path);
        var parsed = WorkflowParser.Parse(text, resolvedPath);
        if (!parsed.Success)
        {
            foreach (var diagnostic in parsed.Diagnostics)
                Console.Error.WriteLine($"invalid: {diagnostic}");
            return 2;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        var runner = new WorkflowRunner(
            new WindowBinder(),
            new WindowsInputExecutor(),
            new ForegroundMonitor(),
            new ScreenCaptureService(),
            new ExactImageComparer(),
            new SystemClock());

        var result = await runner.ExecuteAsync(
            parsed.Definition!,
            new RunOptions(variables, dryRun, output ?? $"pointpilot-traces/{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}", MachineInfoBuilder.Build()),
            cancellation.Token);

        Console.Write(result.Summary);
        return result.Trace.Status switch
        {
            "Completed" => 0,
            "Cancelled" => 4,
            _ => 3
        };
    }

    private static (string Text, string Path) ReadWorkflow(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            Console.Error.WriteLine($"pointpilot: workflow file not found: {fullPath}");
            Environment.Exit(1);
        }
        return (File.ReadAllText(fullPath), fullPath);
    }

    private static string[] Rest(string[] args) => args.Length <= 1 ? [] : args[1..];

    private static int Usage()
    {
        Console.WriteLine("""
            PointPilot — deterministic Windows desktop workflow runner

            usage:
              pointpilot validate <workflow.yaml>
                  Parse and validate a workflow; prints steps and weak-target warnings.

              pointpilot run <workflow.yaml> [--var name=value]... [--dry-run] [--out <dir>]
                  Execute a workflow against its declared target application.

            exit codes:
              0 completed   2 invalid workflow   3 run failed   4 cancelled   1 usage error
            """);
        return 0;
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'. See 'pointpilot --help'.");
        return 1;
    }
}
