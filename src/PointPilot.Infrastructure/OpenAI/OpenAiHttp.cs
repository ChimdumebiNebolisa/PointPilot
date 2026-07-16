using System.Net;
using System.Net.Http.Headers;
using PointPilot.Core;

namespace PointPilot.Infrastructure.OpenAI;

internal sealed class OpenAiHttp(HttpClient client, OpenAiOptions options)
{
    internal async Task<string> PostJsonAsync(string relativePath, string json, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseUri, relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.Add("OpenAI-Safety-Identifier", "pointpilot-local-session");
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var failure = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => IntegrationFailure.InvalidApiKey,
                HttpStatusCode.TooManyRequests => IntegrationFailure.RateLimited,
                _ => IntegrationFailure.Responses
            };
            var safe = ErrorMapper.Map(failure);
            throw new OpenAiIntegrationException(safe, response.StatusCode, SecretRedactor.Redact(body));
        }
        return body;
    }
}
public sealed class OpenAiIntegrationException(SafeError safeError, HttpStatusCode statusCode, string safeDiagnostic)
    : Exception(safeError.WhatFailed)
{
    public SafeError SafeError { get; } = safeError;
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string SafeDiagnostic { get; } = safeDiagnostic;
}
