namespace ViAiStudio.AiGenerator.Providers;

public sealed record ModelRequest(string Provider, string Model, string BaseUrl, string ApiKey, string SystemPrompt, string Prompt);

public sealed record ModelResult(string Text, int TokensIn, int TokensOut);

/// <summary>A single model call, provider-agnostic from the caller's point of view.</summary>
public interface IModelProvider
{
    Task<ModelResult> GenerateAsync(ModelRequest request, CancellationToken cancellationToken);
}
