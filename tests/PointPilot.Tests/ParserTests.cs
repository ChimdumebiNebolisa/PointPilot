using PointPilot.Core.Workflows;

namespace PointPilot.Tests;

public sealed class ParserTests
{
    private const string Minimal = """
        schemaVersion: 1
        name: demo
        target:
          processName: Notepad
        steps:
          - step: focus-window
        """;

    [Fact]
    public void MinimalWorkflow_ParsesWithDefaults()
    {
        var result = WorkflowParser.Parse(Minimal, "test.yaml");
        Assert.True(result.Success);
        var definition = result.Definition!;
        Assert.Equal(1, definition.SchemaVersion);
        Assert.Equal("demo", definition.Name);
        Assert.Equal("Notepad", definition.Target.ProcessName);
        Assert.Equal(ProcessMatchMode.Exact, definition.Target.ProcessNameMatch);
        Assert.Single(definition.Steps);
        Assert.Equal(5000, definition.Defaults.TimeoutMs);
    }

    [Fact]
    public void UnsupportedSchemaVersion_IsRejected()
    {
        var text = Minimal.Replace("schemaVersion: 1", "schemaVersion: 2");
        var result = WorkflowParser.Parse(text, "test.yaml");
        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Path == "schemaVersion" && d.Message.Contains("'2'"));
    }

    [Fact]
    public void NonIntegerSchemaVersion_IsRejected()
    {
        var text = Minimal.Replace("schemaVersion: 1", "schemaVersion: latest");
        Assert.False(WorkflowParser.Parse(text, "t").Success);
    }

    [Fact]
    public void UnknownStepKind_IsRejectedWithKnownKindsListed()
    {
        var text = Minimal.Replace("- step: focus-window", "- step: teleport-control");
        var result = WorkflowParser.Parse(text, "test.yaml");
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("'teleport-control' is not a known step kind"));
    }

    [Fact]
    public void UnknownField_Anywhere_IsRejected()
    {
        var text = """
            schemaVersion: 1
            name: demo
            surprise: true
            target:
              processName: Notepad
            steps:
              - step: click
                selector: { automationId: go }
                momentum: high
            """;
        var diagnostics = WorkflowParser.Parse(text, "test.yaml").Diagnostics;
        Assert.Contains(diagnostics, d => d.Path == "workflow" && d.Message.Contains("surprise"));
        Assert.Contains(diagnostics, d => d.Path == "steps[0]" && d.Message.Contains("momentum"));
    }

    [Fact]
    public void CoordinateSelector_MissingY_IsRejected()
    {
        var text = """
            schemaVersion: 1
            name: demo
            target:
              processName: Notepad
            steps:
              - step: click
                selector: { x: 40 }
            """;
        Assert.Contains(WorkflowParser.Parse(text, "t").Diagnostics, d => d.Path == "steps[0].selector" && d.Message.Contains("non-negative x and y"));
    }

    [Fact]
    public void CoordinateCombinedWithCriteria_IsRejected()
    {
        var text = """
            schemaVersion: 1
            name: demo
            target:
              processName: Notepad
            steps:
              - step: click
                selector: { x: 4, y: 5, name: Save }
            """;
        Assert.Contains(WorkflowParser.Parse(text, "t").Diagnostics, d => d.Message.Contains("cannot be combined"));
    }

    [Theory]
    [InlineData("first", true)]
    [InlineData("index:2", true)]
    [InlineData("last", false)]
    [InlineData("index:-1", false)]
    public void PickValidation(string pick, bool valid)
    {
        var text = $$"""
            schemaVersion: 1
            name: demo
            target:
              processName: Notepad
            steps:
              - step: assert-control
                selector: { className: Cell, pick: '{{pick}}' }
                state: exists
            """;
        Assert.Equal(valid, WorkflowParser.Parse(text, "t").Success);
    }

    [Fact]
    public void PressWithoutKeys_IsRejected()
    {
        var text = """
            schemaVersion: 1
            name: demo
            target:
              processName: Notepad
            steps:
              - step: press
                keys: []
            """;
        Assert.Contains(WorkflowParser.Parse(text, "t").Diagnostics, d => d.Path.EndsWith(".keys") && d.Message.Contains("between 1 and 8"));
    }

    [Fact]
    public void WaitWithoutUntil_IsRejected()
    {
        var text = """
            schemaVersion: 1
            name: demo
            target:
              processName: Notepad
            steps:
              - step: wait
                timeoutMs: 250
            """;
        Assert.Contains(WorkflowParser.Parse(text, "t").Diagnostics, d => d.Path.EndsWith(".until"));
    }

    [Fact]
    public void DelayOutsideBounds_IsRejected()
    {
        var text = """
            schemaVersion: 1
            name: demo
            target:
              processName: Notepad
            steps:
              - step: wait
                until: { delayMs: 120000 }
            """;
        Assert.Contains(WorkflowParser.Parse(text, "t").Diagnostics, d => d.Message.Contains("between 1 and 60000"));
    }

    [Fact]
    public void AssertControlStateValue_RequiresValueField()
    {
        var text = """
            schemaVersion: 1
            name: demo
            target:
              processName: Notepad
            steps:
              - step: assert-control
                selector: { automationId: total }
                state: value
            """;
        Assert.Contains(WorkflowParser.Parse(text, "t").Diagnostics, d => d.Path.EndsWith(".value") && d.Message.Contains("requires a 'value' field"));
    }

    [Fact]
    public void InvalidTargetRegex_IsRejectedSemantically()
    {
        var text = """
            schemaVersion: 1
            name: demo
            target:
              processName: Notepad
              windowTitleRegex: "(unclosed"
            steps:
              - step: focus-window
            """;
        Assert.Contains(WorkflowParser.Parse(text, "t").Diagnostics, d => d.Path == "target.windowTitleRegex" && d.Message.Contains("Invalid regular expression"));
    }

    [Fact]
    public void EmptyStepsList_IsRejected()
    {
        var text = """
            schemaVersion: 1
            name: demo
            target:
              processName: Notepad
            steps: []
            """;
        Assert.Contains(WorkflowParser.Parse(text, "t").Diagnostics, d => d.Message.Contains("at least one step"));
    }

    [Fact]
    public void MalformedYaml_ReportsLineInformation()
    {
        const string text = "schemaVersion: 1\nname: [\n  broken";
        var diagnostics = WorkflowParser.Parse(text, "broken.yaml").Diagnostics;
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Path == "document" || d.Path.StartsWith("name") || d.Message.Contains("valid YAML"));
    }
}
