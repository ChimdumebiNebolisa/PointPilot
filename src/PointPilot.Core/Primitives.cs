namespace PointPilot.Core;

public readonly record struct ScreenPoint(int X, int Y);

public readonly record struct WindowBounds(int Left, int Top, int Width, int Height)
{
    public int Right => checked(Left + Width);
    public int Bottom => checked(Top + Height);
    public bool IsValid => Width > 0 && Height > 0;
    public bool Contains(ScreenPoint point) => IsValid && point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;
}
public static class CoordinateMapper
{
    public static ScreenPoint ImageToScreen(ScreenPoint imagePoint, int imageWidth, int imageHeight, WindowBounds window)
    {
        if (imageWidth <= 0 || imageHeight <= 0 || !window.IsValid)
            throw new ArgumentOutOfRangeException(nameof(imageWidth), "Image and window dimensions must be positive.");
        if (imagePoint.X < 0 || imagePoint.Y < 0 || imagePoint.X >= imageWidth || imagePoint.Y >= imageHeight)
            throw new ArgumentOutOfRangeException(nameof(imagePoint), "Image coordinate is outside the captured image.");

        var x = window.Left + (int)Math.Round(imagePoint.X * (window.Width / (double)imageWidth), MidpointRounding.AwayFromZero);
        var y = window.Top + (int)Math.Round(imagePoint.Y * (window.Height / (double)imageHeight), MidpointRounding.AwayFromZero);
        return new ScreenPoint(Math.Min(x, window.Right - 1), Math.Min(y, window.Bottom - 1));
    }
}

public static class KeyNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> Keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["RETURN"] = "ENTER",
        ["ESC"] = "ESCAPE",
        ["DEL"] = "DELETE",
        ["ARROWUP"] = "UP",
        ["ARROWDOWN"] = "DOWN",
        ["ARROWLEFT"] = "LEFT",
        ["ARROWRIGHT"] = "RIGHT",
        ["CONTROL"] = "CTRL",
        ["CMD"] = "WIN",
        ["COMMAND"] = "WIN",
        ["META"] = "WIN",
        ["OPTION"] = "ALT"
    };

    public static string Normalize(string key)
    {
        var value = key?.Trim() ?? throw new ArgumentNullException(nameof(key));
        if (value.Length == 0) throw new ArgumentException("Key cannot be empty.", nameof(key));
        return Keys.TryGetValue(value, out var normalized) ? normalized : value.ToUpperInvariant();
    }
}
