using PointPilot.Core.Engine;using PointPilot.Core.Tracing;
using System.Windows.Forms;

namespace PointPilot.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public Task Delay(int milliseconds, CancellationToken cancellationToken) => Task.Delay(milliseconds, cancellationToken);
}

public static class MachineInfoBuilder
{
    public static MachineInfo Build()
    {
        var bounds = Screen.PrimaryScreen?.Bounds;
        return new MachineInfo(
            Environment.OSVersion.VersionString,
            Environment.Version.ToString(),
            Environment.Is64BitProcess,
            bounds?.Width ?? 0,
            bounds?.Height ?? 0);
    }
}
