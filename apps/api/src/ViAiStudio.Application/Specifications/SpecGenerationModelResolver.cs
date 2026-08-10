using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

/// <summary>
/// Shared model lookup for AI calls made while authoring a specification in
/// the wizard (phase text, keyword chips, ...). Prefers whatever is routed
/// to <see cref="AiTaskType.SpecGeneration"/>, falling back to
/// <see cref="AiTaskType.CodeGeneration"/> (the default coding model) when
/// Spec generation has no routing of its own. Resolution happens entirely
/// from the database -- AI Generator itself is never asked to make this
/// decision, it only ever receives the credentials this resolves to.
/// </summary>
public sealed class SpecGenerationModelResolver(
    ITaskRoutingRepository taskRoutingRepository,
    IAiModelConfigRepository aiModelConfigRepository)
{
    public async Task<AiModelConfig> ResolveAsync(CancellationToken cancellationToken)
    {
        var specRouting = await taskRoutingRepository.GetAsync(AiTaskType.SpecGeneration, cancellationToken);
        var configId = specRouting?.AiModelConfigId;

        if (configId is null)
        {
            var codeRouting = await taskRoutingRepository.GetAsync(AiTaskType.CodeGeneration, cancellationToken);
            configId = codeRouting?.AiModelConfigId;
        }

        if (configId is null)
        {
            throw new InvalidOperationException(
                "No model is routed to Spec generation or Code generation yet. Configure one in Admin → AI model configuration.");
        }

        return await aiModelConfigRepository.GetAsync(configId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Routed model configuration '{configId}' no longer exists.");
    }
}
