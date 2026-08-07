using ViAiStudio.Application.Common;

namespace ViAiStudio.Application.Builds;

public sealed record RecordBuildProgressCommand(Guid GenerationId, string StepLabel, string LogLine, int ProgressPct);

/// <summary>Webhook target for AI Generator's per-step progress events during a build.</summary>
public sealed class RecordBuildProgressHandler(
    IGenerationRepository generationRepository,
    ISpecificationRepository specificationRepository,
    IBuildEventBroadcaster broadcaster)
{
    public async Task HandleAsync(RecordBuildProgressCommand command, CancellationToken cancellationToken)
    {
        var generation = await generationRepository.GetAsync(command.GenerationId, cancellationToken)
            ?? throw new InvalidOperationException($"Generation '{command.GenerationId}' does not exist.");

        var specification = await specificationRepository.GetAsync(generation.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{generation.SpecificationId}' does not exist.");

        specification.Progress = Math.Clamp(command.ProgressPct, 0, 99);
        await specificationRepository.SaveChangesAsync(cancellationToken);

        broadcaster.Publish(new BuildEvent(generation.Id, command.StepLabel, command.LogLine, specification.Progress, Done: false));
    }
}
