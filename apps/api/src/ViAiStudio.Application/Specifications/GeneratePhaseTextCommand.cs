using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Catalog;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record GeneratePhaseTextCommand(Guid SpecificationId, int PhaseIndex);

/// <summary>
/// Drives the wizard's "Generate" button: builds a prompt from the phase
/// definition plus whatever the user has checked/selected, routes it to the
/// model configured for <see cref="AiTaskType.SpecGeneration"/>, and stores
/// the result back on the phase.
/// </summary>
public sealed class GeneratePhaseTextHandler(
    ISpecificationRepository specificationRepository,
    ITaskRoutingRepository taskRoutingRepository,
    IAiModelConfigRepository aiModelConfigRepository,
    IAiCallLogRepository aiCallLogRepository,
    IAiGeneratorClient aiGeneratorClient)
{
    public async Task<SpecificationPhase> HandleAsync(GeneratePhaseTextCommand command, CancellationToken cancellationToken)
    {
        var definition = SpecificationPhaseCatalog.ByIndex(command.PhaseIndex);

        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        var phase = specification.Phases.SingleOrDefault(p => p.PhaseIndex == command.PhaseIndex)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' has no phase at index {command.PhaseIndex}.");

        var routing = await taskRoutingRepository.GetAsync(AiTaskType.SpecGeneration, cancellationToken);
        var configId = routing?.AiModelConfigId
            ?? throw new InvalidOperationException("No model is routed to Spec generation yet. Configure one in Admin → AI model configuration.");
        var config = await aiModelConfigRepository.GetAsync(configId, cancellationToken)
            ?? throw new InvalidOperationException($"Routed model configuration '{configId}' no longer exists.");

        const string systemPrompt = "You draft one phase of a software project specification as concise markdown.";
        var prompt = BuildPrompt(specification, definition, phase);

        var generated = await aiGeneratorClient.GenerateTextAsync(
            ModelCredentials.FromConfig(config), systemPrompt, prompt, cancellationToken);

        phase.GeneratedText = generated.Text;

        await aiCallLogRepository.AddAsync(new AiCallLog
        {
            Id = Guid.NewGuid(),
            SpecificationId = specification.Id,
            GenerationVersion = null,
            Task = AiTaskType.SpecGeneration,
            Model = config.Label,
            TokensIn = generated.TokensIn,
            TokensOut = generated.TokensOut,
            Prompt = prompt,
            Result = generated.Text,
            CreatedAt = DateTimeOffset.UtcNow,
        }, cancellationToken);

        await specificationRepository.SaveChangesAsync(cancellationToken);
        await aiCallLogRepository.SaveChangesAsync(cancellationToken);

        return phase;
    }

    private static string BuildPrompt(Specification specification, SpecificationPhaseDefinition definition, SpecificationPhase phase)
    {
        var lines = new List<string>
        {
            $"Project: {specification.Name}",
            $"Phase {definition.Index + 1} of {SpecificationPhaseCatalog.Phases.Count} — {definition.Title}",
            $"Produces: {definition.Output}",
        };
        if (phase.CheckedItems.Count > 0) lines.Add("Checklist items covered: " + string.Join(", ", phase.CheckedItems));
        if (phase.SelectedKeywords.Count > 0) lines.Add("Keywords: " + string.Join(", ", phase.SelectedKeywords));
        return string.Join('\n', lines);
    }
}
