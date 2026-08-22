using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ViAiStudio.Application.Common;

namespace ViAiStudio.Infrastructure.AiGenerator;

public sealed class AiGeneratorHttpClient(HttpClient httpClient, IConfiguration configuration) : IAiGeneratorClient
{
    // System.Net.Http.Json's defaults are PascalCase; AI Generator's Minimal
    // API endpoints use ASP.NET Core's Web defaults (camelCase), so this has
    // to opt in explicitly or every field round-trips as null.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record GenerateTextRequestBody(string Provider, string Model, string BaseUrl, string ApiKey, string SystemPrompt, string Prompt);
    private sealed record GenerateTextResponseBody(string Text, int TokensIn, int TokensOut);

    private sealed record StackBody(string Backend, string Ui, string Database, string Infra, string UiStyle);
    private sealed record SpecDocumentBody(
        string Path, string Content, string SpecId, string Title, string Component,
        IReadOnlyList<string> DependsOn, IReadOnlyList<string> Provides, IReadOnlyList<string> Generates);
    private sealed record StartBuildRequestBody(
        Guid GenerationId, string Provider, string Model, string BaseUrl, string ApiKey,
        string SpecificationName, string Summary, string Description, string Audience, string Features,
        string SpecMarkdown, StackBody Stack, IReadOnlyList<SpecDocumentBody> Documents, string CallbackBaseUrl);
    private sealed record StartBuildResponseBody(string JobId);

    private sealed record ErrorResponse(string? Detail);

    public async Task<GeneratedText> GenerateTextAsync(
        ModelCredentials credentials, string systemPrompt, string prompt, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/v1/generate/text",
            new GenerateTextRequestBody(credentials.Provider.ToString(), credentials.Model, credentials.BaseUrl, credentials.ApiKey, systemPrompt, prompt),
            JsonOptions,
            cancellationToken);

        await ThrowIfError(response, cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<GenerateTextResponseBody>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("AI Generator returned an empty response.");

        return new GeneratedText(body.Text, body.TokensIn, body.TokensOut);
    }

    public async Task<BuildDispatchResult> StartBuildAsync(
        Guid generationId, ModelCredentials credentials, BuildSpecification specification, CancellationToken cancellationToken)
    {
        var callbackBaseUrl = configuration["Api:PublicBaseUrl"]
            ?? throw new InvalidOperationException("Api:PublicBaseUrl must be configured so AI Generator can report build progress back.");

        var stack = specification.Stack;
        var response = await httpClient.PostAsJsonAsync(
            "/v1/builds",
            new StartBuildRequestBody(
                generationId, credentials.Provider.ToString(), credentials.Model, credentials.BaseUrl, credentials.ApiKey,
                specification.Name, specification.Summary, specification.Description, specification.Audience,
                specification.Features, specification.SpecMarkdown,
                new StackBody(stack.Backend, stack.Ui, stack.Database, stack.Infra, stack.UiStyle),
                specification.Documents
                    .Select(d => new SpecDocumentBody(
                        d.Path, d.Content, d.SpecId, d.Title, d.Component, d.DependsOn, d.Provides, d.Generates))
                    .ToList(),
                callbackBaseUrl),
            JsonOptions,
            cancellationToken);

        await ThrowIfError(response, cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<StartBuildResponseBody>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("AI Generator returned an empty response.");

        return new BuildDispatchResult(body.JobId);
    }

    private static async Task ThrowIfError(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions, cancellationToken);
        throw new InvalidOperationException(error?.Detail ?? $"AI Generator returned {(int)response.StatusCode}.");
    }
}
