using System.Text.Json;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

/// <summary>Serves the stage-1 wizard shell: the chip groups, straight from the prompt library. No AI call.</summary>
public sealed class ListChipGroupsHandler(ISpecificationPromptLibraryRepository promptLibrary)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ChipGroup>> HandleAsync(CancellationToken cancellationToken)
    {
        var rows = await promptLibrary.ListAsync(SpecificationPromptStage.ChipSelection, "chip-group", cancellationToken);
        return rows
            .Select(row => JsonSerializer.Deserialize<ChipGroup>(row.Content, JsonOptions)
                ?? throw new InvalidOperationException($"Prompt template '{row.Key}' is not a valid chip group."))
            .ToList();
    }
}
