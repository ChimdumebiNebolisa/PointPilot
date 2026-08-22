using System.Text.RegularExpressions;

namespace PointPilot.Core;

/// <summary>
/// Defense-in-depth redaction applied before any diagnostic text is persisted or logged.
/// Traces are built without secrets by construction; this guards against future leaks.
/// </summary>
public static partial class SecretRedactor
{
    [GeneratedRegex(@"(?i)(authorization\s*:\s*bearer\s+)[^\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex BearerHeader();

    [GeneratedRegex(@"\b(?:sk|xox|ghp|gho)-[A-Za-z0-9_-]{8,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex CommonToken();

    public static string Redact(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return BearerHeader().Replace(CommonToken().Replace(value, "[REDACTED]"), "$1[REDACTED]");
    }
}
