using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Api.Contracts;

/// <summary>
/// The API key is masked by default; callers that need it revealed (e.g. an
/// admin clicking "Reveal") use GET /api/admin/ai-configs/{id}/reveal.
/// </summary>
public sealed record AiModelConfigResponse(Guid Id, string Label, AiProvider Provider, string Model, string BaseUrl, string MaskedApiKey)
{
    public static AiModelConfigResponse FromEntity(AiModelConfig config) =>
        new(config.Id, config.Label, config.Provider, config.Model, config.BaseUrl, Mask(config.ApiKey));

    private static string Mask(string apiKey)
    {
        if (apiKey.Length <= 8)
        {
            return new string('•', apiKey.Length);
        }

        return $"{apiKey[..4]}{new string('•', 6)}{apiKey[^4..]}";
    }
}

public sealed record TaskRoutingResponse(AiTaskType Task, Guid? AiModelConfigId)
{
    public static TaskRoutingResponse FromEntity(TaskRouting routing) => new(routing.Task, routing.AiModelConfigId);
}
