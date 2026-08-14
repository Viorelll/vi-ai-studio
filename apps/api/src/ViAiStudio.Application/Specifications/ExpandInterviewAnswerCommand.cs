using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record ExpandInterviewAnswerCommand(Guid SpecificationId, string QuestionText, string AnswerText);

/// <summary>
/// The one-shot AI helper behind the interview stage's per-field "tighten"
/// button -- same one-call shape as <see cref="GeneratePhaseChipsHandler"/>,
/// just with its system prompt sourced from the prompt library instead of a
/// literal.
/// </summary>
public sealed class ExpandInterviewAnswerHandler(
    ISpecificationRepository specificationRepository,
    ISpecificationPromptLibraryRepository promptLibrary,
    SpecGenerationModelResolver modelResolver,
    IAiCallLogRepository aiCallLogRepository,
    IAiGeneratorClient aiGeneratorClient)
{
    public async Task<string> HandleAsync(ExpandInterviewAnswerCommand command, CancellationToken cancellationToken)
    {
        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        var systemPrompt = await promptLibrary.GetAsync("interview.expand-helper.system", cancellationToken)
            ?? throw new InvalidOperationException("Prompt template 'interview.expand-helper.system' is missing.");

        var config = await modelResolver.ResolveAsync(cancellationToken);
        var prompt = $"Interview question: {command.QuestionText}\n\nMy answer: {command.AnswerText}";

        var generated = await aiGeneratorClient.GenerateTextAsync(
            ModelCredentials.FromConfig(config), systemPrompt.Content, prompt, cancellationToken);

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
        await aiCallLogRepository.SaveChangesAsync(cancellationToken);

        return generated.Text.Trim();
    }
}
