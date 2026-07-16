using PointPilot.Core;
using PointPilot.Infrastructure;

namespace PointPilot.Tests;

public sealed class VerificationServiceTests
{
    [Fact]
    public async Task ExistingUnchangedExportFile_IsNotAcceptedAsFreshEvidence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pointpilot-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(path, [1, 2, 3]);
        try
        {
            var file = new FileInfo(path);
            var checkpoint = new FileCheckpoint(true, file.Length, file.LastWriteTimeUtc);
            var service = new VerificationService(new CertainVisual());
            var result = await service.VerifyAsync("Export PNG", Fixture.Snapshot(png: [1]), Fixture.Snapshot(png: [2]), path, checkpoint, CancellationToken.None);
            Assert.False(result.Succeeded);
            Assert.Contains("not observably replaced", result.Summary, StringComparison.Ordinal);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task NewExpectedFileAndVisibleChange_AreAcceptedTogether()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pointpilot-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(path, [1, 2, 3]);
        try
        {
            var service = new VerificationService(new CertainVisual());
            var result = await service.VerifyAsync("Export PNG", Fixture.Snapshot(png: [1]), Fixture.Snapshot(png: [2]), path, new FileCheckpoint(false, 0, DateTimeOffset.MinValue), CancellationToken.None);
            Assert.True(result.Succeeded);
            Assert.Contains(path, result.Evidence!, StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(path); }
    }

    private sealed class CertainVisual : IVisualReasoningService
    {
        public Task<VisualGroundingResult> AnalyzeAsync(string request, WindowSnapshot snapshot, CancellationToken cancellationToken) =>
            Task.FromResult(new VisualGroundingResult("The expected visible result is present.", null, true));
    }
}
