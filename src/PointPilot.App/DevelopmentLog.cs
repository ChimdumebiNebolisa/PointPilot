using System.IO;
using System.Text.Json;
using PointPilot.Core;

namespace PointPilot.App;

internal sealed class DevelopmentLog
{
    private readonly object _sync = new();
    private readonly string _path;

    internal DevelopmentLog()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PointPilot", "logs");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "pointpilot.ndjson");
    }

    internal void Write(string eventName, object metadata)
    {
        var line = SecretRedactor.Redact(JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow, eventName, metadata }));
        lock (_sync)
        {
            if (File.Exists(_path) && new FileInfo(_path).Length > 1_000_000)
                File.Move(_path, _path + ".previous", true);
            File.AppendAllText(_path, line + Environment.NewLine);
        }
    }
}
