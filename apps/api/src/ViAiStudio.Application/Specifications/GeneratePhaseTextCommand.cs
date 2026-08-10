using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Catalog;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record GeneratePhaseTextCommand(Guid SpecificationId, int PhaseIndex);

/// <summary>
/// Drives the wizard's "Generate" button: builds a prompt from the phase
/// definition plus whatever the user has checked/selected, routes it to the
/// model configured for <see cref="AiTaskType.SpecGeneration"/> -- falling
/// back to whatever is routed for <see cref="AiTaskType.CodeGeneration"/>
/// (the default coding model) when Spec generation has no routing of its
/// own, since drafting a spec phase is close enough in kind to reuse it --
/// and stores the result back on the phase.
/// </summary>
public sealed class GeneratePhaseTextHandler(
    ISpecificationRepository specificationRepository,
    SpecGenerationModelResolver modelResolver,
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

        var config = await modelResolver.ResolveAsync(cancellationToken);

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
        var lines = new List<string> { $"Project: {specification.Name}" };
        if (!string.IsNullOrWhiteSpace(specification.Summary)) lines.Add($"Summary: {specification.Summary}");
        if (!string.IsNullOrWhiteSpace(specification.Description)) lines.Add($"Description: {specification.Description}");
        if (!string.IsNullOrWhiteSpace(specification.Features)) lines.Add($"Requirements & features: {specification.Features}");
        if (!string.IsNullOrWhiteSpace(specification.Audience)) lines.Add($"Audience: {specification.Audience}");
        var stack = specification.Stack;
        lines.Add($"Stack: {stack.Backend}, {stack.Ui}, {stack.Database}, {stack.Infra}, {stack.UiStyle}");
        lines.Add($"Phase {definition.Index + 1} of {SpecificationPhaseCatalog.Phases.Count} — {definition.Title}");
        lines.Add($"Produces: {definition.Output}");
        if (phase.CheckedItems.Count > 0) lines.Add("Checklist items covered: " + string.Join(", ", phase.CheckedItems));
        if (phase.SelectedKeywords.Count > 0) lines.Add("Keywords: " + string.Join(", ", phase.SelectedKeywords));
        return string.Join('\n', lines);
    }
}
