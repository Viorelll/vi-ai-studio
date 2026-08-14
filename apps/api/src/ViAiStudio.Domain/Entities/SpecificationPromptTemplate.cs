namespace ViAiStudio.Domain.Entities;

public enum SpecificationPromptStage
{
    ChipSelection,
    DomainInterview,
    Generation,
    Shared,
}

/// <summary>
/// The reusable authoring content the specification wizard is built from --
/// chip groups, interview rounds, authoring rules, the ID scheme, templates
/// and batch instructions -- seeded from specification-example/ (see
/// SpecificationPromptLibrarySeeder) and read by the wizard's backend
/// commands instead of being hardcoded in C# string literals.
/// </summary>
public sealed class SpecificationPromptTemplate
{
    public Guid Id { get; init; }

    /// <summary>Stable lookup key, e.g. "chips.group.c", "generation.batch.5". Never reused.</summary>
    public required string Key { get; init; }
    public required SpecificationPromptStage Stage { get; set; }

    /// <summary>"chip-group" | "interview-round" | "batch-instructions" | "authoring-rule" | "id-scheme" | "template" | "response-format" | "system-prompt".</summary>
    public required string Category { get; set; }
    public required string Title { get; set; }

    /// <summary>JSON for chip-group/interview-round rows (structured options/questions the frontend renders directly); markdown for rule/template/batch rows.</summary>
    public required string Content { get; set; }

    public int OrderIndex { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
