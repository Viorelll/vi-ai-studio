using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record CompleteIntakeInterviewCommand(Guid SpecificationId);

/// <summary>Marks stage 2 done. Gates whether stage 3 (generation) can start.</summary>
public sealed class CompleteIntakeInterviewHandler(ISpecificationRepository specificationRepository)
{
    public async Task<SpecificationIntakeSheet> HandleAsync(CompleteIntakeInterviewCommand command, CancellationToken cancellationToken)
    {
        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        var intake = specification.Intake
            ?? throw new InvalidOperationException("Chip selections must be saved before completing the interview.");

        intake.InterviewCompletedAt = DateTimeOffset.UtcNow;
        await specificationRepository.SaveChangesAsync(cancellationToken);
        return intake;
    }
}
