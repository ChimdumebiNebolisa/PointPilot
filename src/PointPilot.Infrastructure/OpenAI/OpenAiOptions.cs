namespace PointPilot.Infrastructure.OpenAI;

public sealed record OpenAiOptions(string ApiKey, string ResponsesModel, string RealtimeModel, Uri BaseUri)
{
    public static OpenAiOptions Load()
    {
        var values = LoadLocalEnvironment();
        var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? values.GetValueOrDefault("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("OPENAI_API_KEY is not configured.");
        var responses = Environment.GetEnvironmentVariable("POINTPILOT_RESPONSES_MODEL") ?? values.GetValueOrDefault("POINTPILOT_RESPONSES_MODEL") ?? "gpt-5.6";
        var realtime = Environment.GetEnvironmentVariable("POINTPILOT_REALTIME_MODEL") ?? values.GetValueOrDefault("POINTPILOT_REALTIME_MODEL") ?? "gpt-realtime-2.1";
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? values.GetValueOrDefault("OPENAI_BASE_URL") ?? "https://api.openai.com/v1/";
        return new(key.Trim(), responses.Trim(), realtime.Trim(), new Uri(baseUrl, UriKind.Absolute));
    }

    private static Dictionary<string, string> LoadLocalEnvironment()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            for (var depth = 0; directory is not null && depth < 8; depth++, directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, ".env.local");
                if (!File.Exists(path)) continue;
                foreach (var line in File.ReadLines(path))
                {
                    var value = line.Trim();
                    if (value.Length == 0 || value.StartsWith('#')) continue;
                    var separator = value.IndexOf('=');
                    if (separator <= 0) continue;
                    result[value[..separator].Trim()] = value[(separator + 1)..].Trim().Trim('"');
                }
                return result;
            }
        }
        return result;
    }
}
