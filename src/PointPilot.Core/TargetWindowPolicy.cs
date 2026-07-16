namespace PointPilot.Core;

public static class TargetWindowPolicy
{
    public static void ValidateForMutation(WindowSnapshot captured, nint foregroundHandle, string foregroundProcessName, WindowBounds foregroundBounds)
    {
        if (foregroundHandle != captured.Handle) throw new InvalidOperationException("The foreground window changed before input execution.");
        if (!foregroundProcessName.StartsWith("gimp", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Only foreground GIMP is allowlisted for computer control.");
        if (foregroundBounds != captured.Bounds)
            throw new InvalidOperationException("The target window moved or resized; a fresh screenshot is required.");
    }

    public static void ValidateConfirmedText(string? confirmedTargetPath, string windowTitle, string text)
    {
        if (confirmedTargetPath is null) return;
        var value = text.Trim().Trim('"');
        var fileDialog = windowTitle.Contains("export", StringComparison.OrdinalIgnoreCase) || windowTitle.Contains("save", StringComparison.OrdinalIgnoreCase);
        var pathLike = value.Contains(":\\", StringComparison.Ordinal) || value.StartsWith("\\\\", StringComparison.Ordinal) || value.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        if (!fileDialog && !pathLike) return;
        var allowed = new[]
        {
            confirmedTargetPath,
            Path.GetDirectoryName(confirmedTargetPath),
            Path.GetFileName(confirmedTargetPath),
            Path.GetFileNameWithoutExtension(confirmedTargetPath)
        };
        if (!allowed.Any(candidate => string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Typed export text does not match the exact confirmed target.");
    }
}
