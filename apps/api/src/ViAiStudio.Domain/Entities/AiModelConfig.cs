namespace ViAiStudio.Domain.Entities;

public enum AiProvider
{
    OpenAi,
    Anthropic,
    Google,
    Mistral,
    ElevenLabs,
    AzureOpenAi,
    Custom,
}

/// <summary>
/// A configured connection to a model provider (label, provider, model name,
/// base URL, API key). Admins wire these up in AI model configuration and
/// assign one to each <see cref="AiTaskType"/> via <see cref="TaskRouting"/>.
/// The API key is stored so it can be handed to the AI Generator service
/// in-flight; AI Generator itself holds no credentials at rest.
/// </summary>
public sealed class AiModelConfig
{
    public Guid Id { get; init; }
    public required string Label { get; set; }
    public required AiProvider Provider { get; set; }
    public required string Model { get; set; }
    public required string BaseUrl { get; set; }
    public required string ApiKey { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
