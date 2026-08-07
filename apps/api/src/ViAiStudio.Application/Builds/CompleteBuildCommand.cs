using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Builds;

public sealed record BuildAiCallReport(string Model, int TokensIn, int TokensOut, string Prompt, string Result);

public sealed record CompleteBuildCommand(
    Guid GenerationId,
    bool Success,
    string? FailureReason,
    int DurationSeconds,
    List<string> FileTree,
    string? ArchiveStorageKey,
    List<BuildAiCallReport> AiCalls);

/// <summary>Webhook target for AI Generator reporting that a build finished (or failed).</summary>
public sealed class CompleteBuildHandler(
    IGenerationRepository generationRepository,
    ISpecificationRepository specificationRepository,
    IAiCallLogRepository aiCallLogRepository,
    IBuildEventBroadcaster broadcaster)
{
    public async Task<Generation> HandleAsync(CompleteBuildCommand command, CancellationToken cancellationToken)
    {
        var generation = await generationRepository.GetAsync(command.GenerationId, cancellationToken)
            ?? throw new InvalidOperationException($"Generation '{command.GenerationId}' does not exist.");

        var specification = await specificationRepository.GetAsync(generation.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{generation.SpecificationId}' does not exist.");

        generation.Status = command.Success ? GenerationStatus.Ready : GenerationStatus.Failed;
        generation.Note = command.Success ? generation.Note : (command.FailureReason ?? "Build failed");
        generation.DurationSeconds = command.DurationSeconds;
        generation.FileTree = command.FileTree;
        generation.ArchiveStorageKey = command.ArchiveStorageKey;

        specification.Status = command.Success ? SpecificationStatus.Ready : SpecificationStatus.Failed;
        specification.Progress = command.Success ? 100 : specification.Progress;

        foreach (var call in command.AiCalls)
        {
            await aiCallLogRepository.AddAsync(new AiCallLog
            {
                Id = Guid.NewGuid(),
                SpecificationId = specification.Id,
                GenerationVersion = generation.Version,
                Task = AiTaskType.CodeGeneration,
                Model = call.Model,
                TokensIn = call.TokensIn,
                TokensOut = call.TokensOut,
                Prompt = call.Prompt,
                Result = call.Result,
                CreatedAt = DateTimeOffset.UtcNow,
            }, cancellationToken);
        }

        await generationRepository.SaveChangesAsync(cancellationToken);
        await specificationRepository.SaveChangesAsync(cancellationToken);
        await aiCallLogRepository.SaveChangesAsync(cancellationToken);

        broadcaster.Publish(new BuildEvent(
            generation.Id,
            StepLabel: "Done",
            LogLine: command.Success ? "Deployment successful ✓" : $"Build failed: {generation.Note}",
            ProgressPct: specification.Progress,
            Done: true));

        return generation;
    }
}
