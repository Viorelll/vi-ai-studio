using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Builds;

public sealed record StartBuildCommand(Guid SpecificationId, Guid? AiModelConfigId);

/// <summary>
/// Starts an AI Build run: allocates the next <see cref="Generation"/>
/// version, flips the specification to Building, and dispatches the job to
/// AI Generator. AI Generator reports progress and completion back
/// asynchronously via the internal build-events webhook.
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

        if (specification.Status == SpecificationStatus.Building)
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

        specification.Status = SpecificationStatus.Building;
        specification.Progress = 2;

        await specificationRepository.SaveChangesAsync(cancellationToken);
        await generationRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await aiGeneratorClient.StartBuildAsync(
                generation.Id,
                ModelCredentials.FromConfig(config),
                specification.Name,
                specification.SpecMarkdown!,
                specification.Stack,
                cancellationToken);
        }
        catch (Exception ex)
        {
            generation.Status = GenerationStatus.Failed;
            generation.Note = $"Failed to dispatch to AI Generator: {ex.Message}";
            specification.Status = SpecificationStatus.Failed;
            await generationRepository.SaveChangesAsync(cancellationToken);
            await specificationRepository.SaveChangesAsync(cancellationToken);
        }

        return generation;
    }
}
