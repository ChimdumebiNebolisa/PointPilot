using System.Text.RegularExpressions;

namespace PointPilot.Core;

public static partial class SecretRedactor
{
    [GeneratedRegex(@"\bsk-[A-Za-z0-9_-]{8,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex OpenAiKey();

    [GeneratedRegex(@"(?i)(authorization\s*:\s*bearer\s+)[^\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex BearerHeader();

    public static string Redact(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return BearerHeader().Replace(OpenAiKey().Replace(value, "[REDACTED]"), "$1[REDACTED]");
    }
}
public enum IntegrationFailure { MissingApiKey, InvalidApiKey, RateLimited, Network, Realtime, Responses, MicrophoneDenied, NoMicrophone, Capture, UnsupportedAction, WindowChanged, TargetClosed, CoordinateOutOfBounds, VerificationUncertain, Cancelled, Unknown }

public sealed record SafeError(IntegrationFailure Failure, string WhatFailed, bool ActionMayHaveOccurred, string UserInspection, string SafeNextStep);

public static class ErrorMapper
{
    public static SafeError Map(IntegrationFailure failure) => failure switch
    {
        IntegrationFailure.MissingApiKey => new(failure, "OpenAI API key is missing.", false, "No application action occurred.", "Configure OPENAI_API_KEY and start a new session."),
        IntegrationFailure.InvalidApiKey => new(failure, "OpenAI rejected the API key.", false, "No application action occurred.", "Replace the key and reconnect."),
        IntegrationFailure.RateLimited => new(failure, "OpenAI rate-limited the request.", false, "No pending action will continue.", "Wait, then retry from the current screen."),
        IntegrationFailure.WindowChanged => new(failure, "The foreground target window changed.", false, "Inspect GIMP before resuming.", "Bring the verified GIMP window to the foreground and resume."),
        IntegrationFailure.CoordinateOutOfBounds => new(failure, "A proposed coordinate was outside the target window.", false, "No out-of-bounds input was sent.", "Refresh the screenshot and replan."),
        IntegrationFailure.Cancelled => new(failure, "The user stopped the task.", true, "Inspect the last visible GIMP change and undo if needed.", "Resume with an explicit instruction."),
        IntegrationFailure.VerificationUncertain => new(failure, "PointPilot could not verify the result.", true, "Inspect the visible result and export path.", "Refresh or clarify the expected outcome."),
        _ => new(failure, "PointPilot encountered an integration failure.", true, "Inspect the foreground application for partial changes.", "Return GIMP to a known state and retry.")
    };
}
