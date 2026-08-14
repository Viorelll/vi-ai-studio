using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record StartSpecificationGenerationCommand(Guid SpecificationId);

/// <summary>
/// Kicks off stage 3: plans the ten batches from the intake sheet, creates
/// the run + batch rows (skipped batches marked up front so the frontend can
/// show them struck through immediately), enqueues the run, and returns
/// without waiting for it -- mirrors StartBuildHandler's fire-and-forget shape.
/// </summary>
public sealed class StartSpecificationGenerationHandler(
    ISpecificationRepository specificationRepository,
    ISpecificationGenerationRunRepository runRepository,
    SpecGenerationModelResolver modelResolver,
    ISpecificationGenerationQueue queue)
{
    public async Task<SpecificationGenerationRun> HandleAsync(StartSpecificationGenerationCommand command, CancellationToken cancellationToken)
    {
        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        if (specification.Intake?.CompletedAt is null)
        {
            throw new InvalidOperationException("Chip selections must be completed before generation can start.");
        }
        if (specification.Intake.InterviewCompletedAt is null)
        {
            throw new InvalidOperationException("The domain interview must be completed before generation can start.");
        }

        var config = await modelResolver.ResolveAsync(cancellationToken);
        var plans = SpecificationGenerationBatchPlanner.Plan(specification.Intake);

        var run = new SpecificationGenerationRun
        {
            Id = Guid.NewGuid(),
            SpecificationId = specification.Id,
            Status = SpecificationGenerationRunStatus.Pending,
            Model = config.Label,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var plan in plans)
        {
            run.Batches.Add(new SpecificationGenerationBatch
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                BatchIndex = plan.BatchIndex,
                Name = plan.Name,
                Status = plan.Skip ? SpecificationGenerationBatchStatus.Skipped : SpecificationGenerationBatchStatus.Pending,
                Note = plan.SkipReason ?? string.Empty,
            });
        }

        await runRepository.AddAsync(run, cancellationToken);
        await runRepository.SaveChangesAsync(cancellationToken);

        specification.Status = SpecificationStatus.Building;
        await specificationRepository.SaveChangesAsync(cancellationToken);

        queue.Enqueue(run.Id);
        return run;
    }
}
