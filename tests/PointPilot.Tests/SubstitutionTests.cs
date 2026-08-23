using PointPilot.Core.Workflows;

namespace PointPilot.Tests;

public sealed class SubstitutionTests
{
    private const string Template = """
        schemaVersion: 1
        name: substitution-demo
        variables:
          target_text: hello
          out_path:
            required: true
          layer:
            default: Background
        target:
          processName: "${layer}App"
        steps:
          - step: type-text
            text: "value=${target_text}"
          - step: assert-file
            path: "${out_path}"
            condition: exists
          - step: press
            keys: ["CTRL", "${target_text}"]
        """;

    [Fact]
    public void MissingVariable_IsReportedBeforeAnyRun()
    {
        var parsed = WorkflowParser.Parse(Template, "t");
        Assert.True(parsed.Success);
        var (resolved, diagnostics) = VariableSubstituter.Resolve(parsed.Definition!, new Dictionary<string, string>());
        Assert.Equal("${out_path}", resolved.Steps[1].As<AssertFileStep>().Path);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains("'out_path'"));
    }

    [Fact]
    public void ProvidedAndDefaultVariables_AreSubstitutedEverywhere()
    {
        var parsed = WorkflowParser.Parse(Template, "t");
        var (resolved, diagnostics) = VariableSubstituter.Resolve(parsed.Definition!, new Dictionary<string, string>
        {
            ["out_path"] = @"C:\out\result.png"
        });
        Assert.Empty(diagnostics);
        Assert.Equal("BackgroundApp", resolved.Target.ProcessName);
        Assert.Equal("value=hello", resolved.Steps[0].As<TypeTextStep>().Text);
        Assert.Equal(@"C:\out\result.png", resolved.Steps[1].As<AssertFileStep>().Path);
        Assert.Equal("CTRL", resolved.Steps[2].As<PressStep>().Keys[0]);
        Assert.Equal("hello", resolved.Steps[2].As<PressStep>().Keys[1]);
    }

    [Fact]
    public void SelectorFields_Substitute()
    {
        const string text = """
            schemaVersion: 1
            name: selector-subst
            variables:
              buttonId:
                default: okButton
            target:
              processName: App
            steps:
              - step: click
                selector: { automationId: "${buttonId}" }
            """;
        var (resolved, diagnostics) = VariableSubstituter.Resolve(WorkflowParser.Parse(text, "t").Definition!, new Dictionary<string, string>());
        Assert.Empty(diagnostics);
        Assert.Equal("okButton", resolved.Steps[0].As<ClickStep>().Selector.AutomationId);
    }

    [Fact]
    public void UndeclaredReference_FailsValidationBeforeSubstitution()
    {
        const string text = """
            schemaVersion: 1
            name: undeclared
            target:
              processName: App
            steps:
              - step: type-text
                text: "${nope}"
            """;
        Assert.Contains(WorkflowParser.Parse(text, "t").Diagnostics, d => d.Message.Contains("undeclared variable '${nope}'"));
    }
}

file static class StepExtensions
{
    public static T As<T>(this StepSpec step) where T : StepSpec => (T)step;
}