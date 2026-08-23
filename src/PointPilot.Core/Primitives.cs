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
    // Workflow coordinates are declared relative to the bound window's top-left corner
    // and are always clamped strictly inside the live window bounds.
    public static ScreenPoint RelativeToScreen(ScreenPoint relative, WindowBounds window)
    {
        if (!window.IsValid) throw new ArgumentOutOfRangeException(nameof(window), "The window bounds are invalid.");
        if (relative.X < 0 || relative.Y < 0 || relative.X >= window.Width || relative.Y >= window.Height)
            throw new ArgumentOutOfRangeException(nameof(relative), $"Coordinate ({relative.X}, {relative.Y}) is outside the target window bounds {window.Width}x{window.Height}.");
        return new ScreenPoint(window.Left + relative.X, window.Top + relative.Y);
    }

    public static ScreenPoint ClampIntoCenter(WindowBounds elementBounds, WindowBounds window)
    {
        var cx = elementBounds.Left + elementBounds.Width / 2;
        var cy = elementBounds.Top + elementBounds.Height / 2;
        cx = Math.Clamp(cx, window.Left, window.Right - 1);
        cy = Math.Clamp(cy, window.Top, window.Bottom - 1);
        return new ScreenPoint(cx, cy);
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
