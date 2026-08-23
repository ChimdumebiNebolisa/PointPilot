using PointPilot.Infrastructure.Verification;

namespace PointPilot.Tests;

public sealed class ImageComparerTests
{
    private static byte[] Png(int width, int height, Func<int, int, (int R, int G, int B)> pixel)
    {
        using var bitmap = new System.Drawing.Bitmap(width, height);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var (r, g, b) = pixel(x, y);
                bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(255, r, g, b));
            }
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    [Fact]
    public void IdenticalImages_MatchFully()
    {
        var image = Png(8, 8, (_, _) => (10, 20, 30));
        Assert.Equal(1.0, new ExactImageComparer().MatchFraction(image, image, 0));
    }

    [Fact]
    public void DeltaWithinThreshold_Matches()
    {
        var actual = Png(4, 4, (_, _) => (12, 22, 32));
        var reference = Png(4, 4, (_, _) => (10, 20, 30));
        Assert.Equal(1.0, new ExactImageComparer().MatchFraction(actual, reference, maxChannelDelta: 2));
    }

    [Fact]
    public void DeltaBeyondThreshold_Fails()
    {
        var actual = Png(4, 4, (_, _) => (90, 90, 90));
        var reference = Png(4, 4, (_, _) => (10, 20, 30));
        Assert.True(new ExactImageComparer().MatchFraction(actual, reference, maxChannelDelta: 5) < 0.01);
    }

    [Fact]
    public void SizeMismatch_IsRejectedRatherThanFuzzyCompared()
    {
        var small = Png(2, 2, (_, _) => (0, 0, 0));
        var large = Png(4, 4, (_, _) => (0, 0, 0));
        Assert.ThrowsAny<Exception>(() => new ExactImageComparer().MatchFraction(small, large, 8));
    }
}
