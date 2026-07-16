using System.Security.Cryptography;
using PointPilot.Core;

namespace PointPilot.Infrastructure;

public sealed class VerificationService(IVisualReasoningService visual) : IVerificationService
{
    public async Task<VerificationResult> VerifyAsync(string goal, WindowSnapshot before, WindowSnapshot after, string? expectedFilePath, FileCheckpoint? beforeFile, CancellationToken cancellationToken)
    {
        var changed = !SHA256.HashData(before.PngBytes).AsSpan().SequenceEqual(SHA256.HashData(after.PngBytes));
        if (!changed) return VerificationResult.Uncertain("The foreground image did not visibly change.");
        if (!string.IsNullOrWhiteSpace(expectedFilePath))
        {
            var file = new FileInfo(expectedFilePath);
            if (!file.Exists) return VerificationResult.Uncertain($"The expected PNG does not exist at {expectedFilePath}.");
            if (beforeFile is { Existed: true } && file.Length == beforeFile.Length && file.LastWriteTimeUtc == beforeFile.LastWriteTimeUtc.UtcDateTime)
                return VerificationResult.Uncertain($"The expected PNG existed before the task and was not observably replaced at {expectedFilePath}.");
        }
        var grounding = await visual.AnalyzeAsync($"Verify whether this screenshot visibly supports completion of: {goal}. Be conservative and set certain false on ambiguity.", after, cancellationToken).ConfigureAwait(false);
        return grounding.IsCertain
            ? new(true, true, grounding.Summary, string.IsNullOrWhiteSpace(expectedFilePath) ? "Visible post-action screenshot." : $"Visible post-action screenshot and file exists at {expectedFilePath}.")
            : VerificationResult.Uncertain(grounding.Summary);
    }
}
