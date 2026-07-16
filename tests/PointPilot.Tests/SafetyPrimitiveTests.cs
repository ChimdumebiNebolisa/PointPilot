using PointPilot.Core;

namespace PointPilot.Tests;

public sealed class SafetyPrimitiveTests
{
    [Theory]
    [InlineData("What does this panel do?", ActionPolicyLevel.ReversibleEdit)]
    [InlineData("Export it as a PNG", ActionPolicyLevel.Consequential)]
    [InlineData("Make a PNG", ActionPolicyLevel.Consequential)]
    [InlineData("Open a terminal and publish it", ActionPolicyLevel.Prohibited)]
    public void GoalPolicy_ClassifiesSafetyLevel(string goal, ActionPolicyLevel expected) =>
        Assert.Equal(expected, ActionPolicy.ClassifyGoal(goal));

    [Fact]
    public void CoordinateMapper_MapsPixelsAndRejectsOutOfBounds()
    {
        var bounds = new WindowBounds(100, 200, 1000, 500);
        Assert.Equal(new ScreenPoint(600, 450), CoordinateMapper.ImageToScreen(new ScreenPoint(500, 250), 1000, 500, bounds));
        Assert.Throws<ArgumentOutOfRangeException>(() => CoordinateMapper.ImageToScreen(new ScreenPoint(1000, 0), 1000, 500, bounds));
    }

    [Theory]
    [InlineData("return", "ENTER")]
    [InlineData("Control", "CTRL")]
    [InlineData("ArrowLeft", "LEFT")]
    public void KeyNormalizer_UsesWindowsNames(string input, string expected) => Assert.Equal(expected, KeyNormalizer.Normalize(input));

    [Fact]
    public void SecretRedactor_RemovesKeysAndBearerTokens()
    {
        var result = SecretRedactor.Redact("sk-" + new string('a', 24) + " Authorization: Bearer secret-token");
        Assert.DoesNotContain("abcdefghijklmnopqrstuvwxyz", result, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", result, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void TargetPolicy_RejectsWrongProcessWindowAndBounds()
    {
        var snapshot = Fixture.Snapshot(handle: 42, process: "gimp-3.0", bounds: new WindowBounds(0, 0, 800, 600));
        TargetWindowPolicy.ValidateForMutation(snapshot, 42, "gimp-3.0", snapshot.Bounds);
        Assert.Throws<InvalidOperationException>(() => TargetWindowPolicy.ValidateForMutation(snapshot, 43, "gimp-3.0", snapshot.Bounds));
        Assert.Throws<UnauthorizedAccessException>(() => TargetWindowPolicy.ValidateForMutation(snapshot, 42, "notepad", snapshot.Bounds));
        Assert.Throws<InvalidOperationException>(() => TargetWindowPolicy.ValidateForMutation(snapshot, 42, "gimp-3.0", new WindowBounds(1, 0, 800, 600)));
    }

    [Fact]
    public void ErrorMapper_ReportsCancellationAsPotentiallyPartial()
    {
        var error = ErrorMapper.Map(IntegrationFailure.Cancelled);
        Assert.True(error.ActionMayHaveOccurred);
        Assert.Contains("undo", error.UserInspection, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfirmedExportText_IsBoundInsideFileDialogs()
    {
        var target = Path.GetFullPath("pointpilot.png");
        TargetWindowPolicy.ValidateConfirmedText(target, "Export Image", target);
        TargetWindowPolicy.ValidateConfirmedText(target, "Export Image", "pointpilot");
        Assert.Throws<UnauthorizedAccessException>(() => TargetWindowPolicy.ValidateConfirmedText(target, "Export Image", "other-file"));
        TargetWindowPolicy.ValidateConfirmedText(target, "PointPilot demo — GIMP", "Built for Focus");
    }
}
