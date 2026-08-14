using System.Text.Json;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

/// <summary>Serves the stage-2 wizard shell: the interview round definitions, straight from the prompt library. No AI call.</summary>
public sealed class ListInterviewRoundsHandler(ISpecificationPromptLibraryRepository promptLibrary)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<InterviewRound>> HandleAsync(CancellationToken cancellationToken)
    {
        var rows = await promptLibrary.ListAsync(SpecificationPromptStage.DomainInterview, "interview-round", cancellationToken);
        return rows
            .Select(row => JsonSerializer.Deserialize<InterviewRound>(row.Content, JsonOptions)
                ?? throw new InvalidOperationException($"Prompt template '{row.Key}' is not a valid interview round."))
            .ToList();
    }
}
