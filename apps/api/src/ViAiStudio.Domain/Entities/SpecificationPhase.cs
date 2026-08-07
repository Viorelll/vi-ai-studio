namespace ViAiStudio.Domain.Entities;

/// <summary>
/// The wizard state captured for one phase (see
/// <see cref="Catalog.SpecificationPhaseCatalog"/>) of a specification: which
/// checklist items are checked, which keyword chips are selected, and the
/// AI-generated markdown produced for the phase, if any.
/// </summary>
public sealed class SpecificationPhase
{
    public Guid Id { get; init; }
    public required Guid SpecificationId { get; init; }

    /// <summary>Index into <see cref="Catalog.SpecificationPhaseCatalog.Phases"/>.</summary>
    public required int PhaseIndex { get; init; }

    public List<string> CheckedItems { get; set; } = [];
    public List<string> SelectedKeywords { get; set; } = [];
    public string? GeneratedText { get; set; }
}
