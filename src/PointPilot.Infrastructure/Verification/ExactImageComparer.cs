using System.Drawing;using System.IO;
using System.Drawing.Imaging;
using PointPilot.Core.Engine;
using StepFailureException = PointPilot.Core.Elements.StepFailureException;

namespace PointPilot.Infrastructure.Verification;

/// <summary>
/// Deterministic pixel comparison: both images must have identical dimensions and every
/// pixel must be within maxChannelDelta on all four channels. No fuzzy matching, no
/// hashing approximations — the result is exactly reproducible.
/// </summary>
public sealed class ExactImageComparer : IImageComparer
{
    public double MatchFraction(byte[] actualPng, byte[] referencePng, int maxChannelDelta)
    {
        using var actual = Decode(actualPng);
        using var reference = Decode(referencePng);
        if (actual.Width != reference.Width || actual.Height != reference.Height)
            throw new StepFailureException($"Image assertion compared different sizes (actual {actual.Width}x{actual.Height} vs reference {reference.Width}x{reference.Height}); capture geometry changed.");
        var matching = 0L;
        var total = (long)actual.Width * actual.Height;
        for (var y = 0; y < actual.Height; y++)
        {
            for (var x = 0; x < actual.Width; x++)
            {
                var a = actual.GetPixel(x, y);
                var r = reference.GetPixel(x, y);
                if (Math.Abs(a.A - r.A) <= maxChannelDelta &&
                    Math.Abs(a.R - r.R) <= maxChannelDelta &&
                    Math.Abs(a.G - r.G) <= maxChannelDelta &&
                    Math.Abs(a.B - r.B) <= maxChannelDelta)
                    matching++;
            }
        }
        return total == 0 ? 1.0 : matching / (double)total;
    }

    private static Bitmap Decode(byte[] png)
    {
        try
        {
            using var stream = new MemoryStream(png);
            return new Bitmap(stream);
        }
        catch (ArgumentException ex)
        {
            throw new StepFailureException($"A comparison image could not be decoded as PNG: {ex.Message}");
        }
    }
}
