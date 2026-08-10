using System.Text.Json;

namespace ViAiStudio.AiGenerator.Providers;

/// <summary>
/// Calls a model deployment hosted on Azure AI Foundry / Azure OpenAI via the
/// Responses API. <see cref="ModelRequest.BaseUrl"/> is the *complete*
/// endpoint URL an admin configured for this model in the database -- e.g.
/// "https://my-resource.cognitiveservices.azure.com/openai/responses?api-version=2025-04-01-preview"
/// -- and is posted to as-is: the exact path and api-version vary per
/// resource and per Foundry API surface, so this service doesn't try to
/// rebuild it from a bare resource root the way the Azure OpenAI SDK does.
/// <see cref="ModelRequest.Model"/> is the deployment name, sent in the
/// request body per the Responses API contract.
/// </summary>
public sealed class AzureFoundryModelProvider(HttpClient httpClient) : IModelProvider
{
    public async Task<ModelResult> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.BaseUrl)
        {
            Content = JsonContent.Create(new
            {
                model = request.Model,
                instructions = request.SystemPrompt,
                input = request.Prompt,
            }),
        };
        httpRequest.Headers.Add("api-key", request.ApiKey);

        var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Azure AI Foundry request to '{request.BaseUrl}' failed with {(int)response.StatusCode}: {ExtractErrorMessage(body)}");
        }

        try
        {
            return ParseResult(body);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Azure AI Foundry returned a response '{request.BaseUrl}' didn't recognize: {ex.Message}", ex);
        }
    }

    private static ModelResult ParseResult(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var text = string.Concat(
            root.GetProperty("output").EnumerateArray()
                .Where(item => item.GetProperty("type").GetString() == "message")
                .SelectMany(item => item.GetProperty("content").EnumerateArray())
                .Where(part => part.GetProperty("type").GetString() == "output_text")
                .Select(part => part.GetProperty("text").GetString()));

        var usage = root.GetProperty("usage");
        return new ModelResult(text, usage.GetProperty("input_tokens").GetInt32(), usage.GetProperty("output_tokens").GetInt32());
    }

    private static string ExtractErrorMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message)
                ? message.GetString() ?? body
                : body;
        }
        catch (JsonException)
        {
            return body;
        }
    }
}
