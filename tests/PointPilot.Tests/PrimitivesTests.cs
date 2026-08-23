using PointPilot.Core;

namespace PointPilot.Tests;

public sealed class PrimitivesTests
{
    [Fact]
    public void WindowBounds_ContainmentIsHalfOpenAndValidated()
    {
        var bounds = new WindowBounds(100, 200, 1000, 500);
        Assert.True(bounds.Contains(new ScreenPoint(100, 200)));
        Assert.False(bounds.Contains(new ScreenPoint(1100, 700)));
        Assert.False(new WindowBounds(0, 0, 0, 10).IsValid);
    }

    [Fact]
    public void RelativeCoordinates_MapIntoScreenSpace()
    {
        var bounds = new WindowBounds(100, 200, 800, 600);
        Assert.Equal(new ScreenPoint(140, 240), CoordinateMapper.RelativeToScreen(new ScreenPoint(40, 40), bounds));
    }

    [Theory]
    [InlineData(-1, 40)]
    [InlineData(800, 40)]
    [InlineData(40, -1)]
    [InlineData(40, 600)]
    public void RelativeCoordinateOutsideBounds_IsRejected(int x, int y)
    {
        var bounds = new WindowBounds(100, 200, 800, 600);
        Assert.Throws<ArgumentOutOfRangeException>(() => CoordinateMapper.RelativeToScreen(new ScreenPoint(x, y), bounds));
    }

    [Fact]
    public void ElementCenter_IsClampedInsideLiveWindow()
    {
        var window = new WindowBounds(0, 0, 500, 500);
        var element = new WindowBounds(-20, -20, 60, 60); // partially off-window element
        Assert.Equal(new ScreenPoint(10, 10), CoordinateMapper.ClampIntoCenter(element, window));
    }

    [Theory]
    [InlineData("return", "ENTER")]
    [InlineData("Control", "CTRL")]
    [InlineData("ArrowLeft", "LEFT")]
    [InlineData("esc", "ESCAPE")]
    public void KeyNormalizer_TranslatesToWindowsNames(string input, string expected) =>
        Assert.Equal(expected, KeyNormalizer.Normalize(input));

    [Fact]
    public void SecretRedactor_RemovesBearerTokensAndCommonKeyShapes()
    {
        const string secret = "Authorization: Bearer super-secret-token and sk-abcdefgh12345678";
        var redacted = SecretRedactor.Redact(secret);
        Assert.DoesNotContain("super-secret-token", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-abcdefgh", redacted, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", redacted, StringComparison.Ordinal);
    }
}
