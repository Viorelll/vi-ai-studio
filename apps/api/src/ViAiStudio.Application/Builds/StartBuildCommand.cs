using ViAiStudio.Application.Common;
using ViAiStudio.Application.Specifications;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Builds;

public sealed record StartBuildCommand(Guid SpecificationId, Guid? AiModelConfigId);

/// <summary>
/// Starts an AI Build run: allocates the next <see cref="Generation"/>
/// version and dispatches the job to AI Generator. AI Generator reports
/// progress and completion back asynchronously via the internal
/// build-events webhook, which only ever updates that <see cref="Generation"/>
/// -- the owning <see cref="Specification"/>'s own status is untouched by
/// any of this, since it reflects the specification's own lifecycle, not
/// any one build's outcome.
/// </summary>
public sealed class StartBuildHandler(
    ISpecificationRepository specificationRepository,
    IGenerationRepository generationRepository,
    ITaskRoutingRepository taskRoutingRepository,
    IAiModelConfigRepository aiModelConfigRepository,
    IAiGeneratorClient aiGeneratorClient)
{
    public async Task<Generation> HandleAsync(StartBuildCommand command, CancellationToken cancellationToken)
    {
        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        if (specification.Generations.Any(g => g.Status == GenerationStatus.Running))
        {
            throw new InvalidOperationException("A build is already running for this specification.");
        }

        if (string.IsNullOrWhiteSpace(specification.SpecMarkdown))
        {
            throw new InvalidOperationException("Finalize the specification before starting AI Build.");
        }

        var configId = command.AiModelConfigId
            ?? (await taskRoutingRepository.GetAsync(AiTaskType.CodeGeneration, cancellationToken))?.AiModelConfigId
            ?? throw new InvalidOperationException("No model is routed to Code generation yet. Configure one in Admin → AI model configuration.");
        var config = await aiModelConfigRepository.GetAsync(configId, cancellationToken)
            ?? throw new InvalidOperationException($"Model configuration '{configId}' does not exist.");

        var nextVersion = await generationRepository.GetLatestVersionAsync(specification.Id, cancellationToken) + 1;

        var generation = new Generation
        {
            Id = Guid.NewGuid(),
            SpecificationId = specification.Id,
            Version = nextVersion,
            Status = GenerationStatus.Running,
            Note = "Generated via AI Build",
            Model = config.Label,
            Stack = specification.Stack,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await generationRepository.AddAsync(generation, cancellationToken);

        specification.Progress = 2;

        await specificationRepository.SaveChangesAsync(cancellationToken);
        await generationRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await aiGeneratorClient.StartBuildAsync(
                generation.Id,
                ModelCredentials.FromConfig(config),
                BuildSpecificationFor(specification),
                cancellationToken);
        }
        catch (Exception ex)
        {
            generation.Status = GenerationStatus.Failed;
            generation.Note = $"Failed to dispatch to AI Generator: {ex.Message}";
            await generationRepository.SaveChangesAsync(cancellationToken);
        }

        return generation;
    }

    /// <summary>
    /// The complete brief handed to AI Generator: the authored basics plus both
    /// renderings of the specification -- the single flattened markdown and the
    /// per-phase document set the download bundle is made of -- so the model
    /// generating the project sees everything the wizard captured.
    /// </summary>
    private static BuildSpecification BuildSpecificationFor(Specification specification) => new(
        specification.Name,
        specification.Summary,
        specification.Description,
        specification.Audience,
        specification.Features,
        specification.SpecMarkdown!,
        specification.Stack,
        SpecificationDocumentSet.Build(specification)
            .Select(d => new BuildSpecificationDocument(d.Path, d.Content))
            .ToList());
}
