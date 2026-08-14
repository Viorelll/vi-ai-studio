using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record InterviewAnswerInput(int QuestionIndex, string QuestionText, string DefaultHint, string? AnswerText);

public sealed record SaveInterviewRoundCommand(Guid SpecificationId, int RoundIndex, IReadOnlyList<InterviewAnswerInput> Answers);

/// <summary>Upserts every answer for one stage-2 round in a single save. An empty answer records the shown default instead.</summary>
public sealed class SaveInterviewRoundHandler(ISpecificationRepository specificationRepository)
{
    public async Task<IReadOnlyList<SpecificationInterviewAnswer>> HandleAsync(
        SaveInterviewRoundCommand command, CancellationToken cancellationToken)
    {
        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        var saved = new List<SpecificationInterviewAnswer>();
        foreach (var input in command.Answers)
        {
            var existing = specification.InterviewAnswers.SingleOrDefault(
                a => a.RoundIndex == command.RoundIndex && a.QuestionIndex == input.QuestionIndex);

            var usedDefault = string.IsNullOrWhiteSpace(input.AnswerText);
            var answerText = usedDefault ? input.DefaultHint : input.AnswerText;

            if (existing is null)
            {
                existing = new SpecificationInterviewAnswer
                {
                    Id = Guid.NewGuid(),
                    SpecificationId = specification.Id,
                    RoundIndex = command.RoundIndex,
                    QuestionIndex = input.QuestionIndex,
                    QuestionText = input.QuestionText,
                    DefaultHint = input.DefaultHint,
                };
                await specificationRepository.AddNewChildAsync(existing, cancellationToken);
                specification.InterviewAnswers.Add(existing);
            }

            existing.QuestionText = input.QuestionText;
            existing.DefaultHint = input.DefaultHint;
            existing.AnswerText = answerText;
            existing.UsedDefault = usedDefault;
            saved.Add(existing);
        }

        await specificationRepository.SaveChangesAsync(cancellationToken);
        return saved;
    }
}
