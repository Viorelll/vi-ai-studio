using ViAiStudio.Domain.Entities;
using ViAiStudio.Domain.ValueObjects;

namespace ViAiStudio.Application.Common;

/// <summary>Provider connection details handed to AI Generator for a single call; never persisted by AI Generator itself.</summary>
public sealed record ModelCredentials(AiProvider Provider, string Model, string BaseUrl, string ApiKey)
{
    public static ModelCredentials FromConfig(AiModelConfig config) =>
        new(config.Provider, config.Model, config.BaseUrl, config.ApiKey);
}

public sealed record GeneratedText(string Text, int TokensIn, int TokensOut);

public sealed record BuildDispatchResult(string JobId);

/// <summary>One rendered specification markdown file (see SpecificationDocumentSet).</summary>
public sealed record BuildSpecificationDocument(string Path, string Content);

/// <summary>
/// Everything AI Generator needs to know about *what* to build: the authored
/// specification in full (basics, the rendered markdown, and the per-phase
/// document set) plus the tech stack it must be built on. AI Generator holds
/// no specification state of its own, so this is the complete brief.
/// </summary>
public sealed record BuildSpecification(
    string Name,
    string Summary,
    string Description,
    string Audience,
    string Features,
    string SpecMarkdown,
    TechStack Stack,
    IReadOnlyList<BuildSpecificationDocument> Documents);

/// <summary>
/// Talks to the stateless ViAiStudio.AiGenerator service. The Api never calls
/// a model provider directly -- it resolves credentials from
/// <see cref="AiModelConfig"/> and hands them to AI Generator per request.
/// </summary>
public interface IAiGeneratorClient
{
    Task<GeneratedText> GenerateTextAsync(
        ModelCredentials credentials,
        string systemPrompt,
        string prompt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Kicks off an AI Build run. AI Generator works the pipeline in the
    /// background and reports progress/completion back to the Api's
    /// internal webhook endpoints (see BuildEventsEndpoints), correlated by
    /// <paramref name="generationId"/>.
    /// </summary>
    Task<BuildDispatchResult> StartBuildAsync(
        Guid generationId,
        ModelCredentials credentials,
        BuildSpecification specification,
        CancellationToken cancellationToken);
}
