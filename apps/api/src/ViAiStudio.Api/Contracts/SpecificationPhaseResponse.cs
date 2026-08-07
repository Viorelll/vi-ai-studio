using ViAiStudio.Domain.Catalog;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Api.Contracts;

public sealed record SpecificationPhaseResponse(
    int PhaseIndex,
    string Title,
    IReadOnlyList<string> Items,
    string Output,
    IReadOnlyList<string> CheckedItems,
    IReadOnlyList<string> SelectedKeywords,
    string? GeneratedText)
{
    public static SpecificationPhaseResponse FromEntity(SpecificationPhase phase)
    {
        var definition = SpecificationPhaseCatalog.ByIndex(phase.PhaseIndex);
        return new(phase.PhaseIndex, definition.Title, definition.Items, definition.Output,
            phase.CheckedItems, phase.SelectedKeywords, phase.GeneratedText);
    }
}

/// <summary>The static 15-phase catalog, served once so the web client can render the wizard shell.</summary>
public sealed record SpecificationPhaseDefinitionResponse(int Index, string Title, IReadOnlyList<string> Items, string Output)
{
    public static SpecificationPhaseDefinitionResponse FromDefinition(SpecificationPhaseDefinition definition) =>
        new(definition.Index, definition.Title, definition.Items, definition.Output);
}
