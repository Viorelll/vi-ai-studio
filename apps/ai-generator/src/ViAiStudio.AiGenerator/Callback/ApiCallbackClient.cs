using System.Net.Http.Json;
using System.Text.Json;

namespace ViAiStudio.AiGenerator.Callback;

public sealed record BuildAiCallReport(string Model, int TokensIn, int TokensOut, string Prompt, string Result);

public sealed record CompleteBuildPayload(
    bool Success,
    string? FailureReason,
    int DurationSeconds,
    List<string> FileTree,
    string? ArchiveStorageKey,
    List<BuildAiCallReport> AiCalls);

/// <summary>
/// Reports build progress back to the Api's internal webhook endpoints
/// (see ViAiStudio.Api's BuildEventsEndpoints). Authenticated with a shared
/// secret header rather than user auth -- both services read the same
/// AiGenerator:WebhookSecret value.
/// </summary>
public sealed class ApiCallbackClient(HttpClient httpClient, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task ReportProgressAsync(string callbackBaseUrl, Guid generationId, string stepLabel, string logLine, int progressPct, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{callbackBaseUrl.TrimEnd('/')}/api/internal/builds/{generationId}/events")
        {
            Content = JsonContent.Create(new { stepLabel, logLine, progressPct }, options: JsonOptions),
        };
        AddSecret(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReportCompleteAsync(string callbackBaseUrl, Guid generationId, CompleteBuildPayload payload, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{callbackBaseUrl.TrimEnd('/')}/api/internal/builds/{generationId}/complete")
        {
            Content = JsonContent.Create(payload, options: JsonOptions),
        };
        AddSecret(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void AddSecret(HttpRequestMessage request)
    {
        var secret = configuration["AiGenerator:WebhookSecret"];
        if (!string.IsNullOrEmpty(secret))
        {
            request.Headers.Add("X-Internal-Secret", secret);
        }
    }
}
