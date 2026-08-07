using ViAiStudio.Domain.ValueObjects;

namespace ViAiStudio.Domain.Entities;

public enum SpecificationStatus
{
    Draft,
    Building,
    Ready,
    Failed,
}

/// <summary>
/// A project specification authored in AI Specification Studio. Starts as a
/// Draft while its 15 wizard phases are filled in, moves to Building while
/// AI Build is generating a project from it, and settles on Ready or Failed
/// once a <see cref="Generation"/> completes.
/// </summary>
public sealed class Specification
{
    public Guid Id { get; init; }
    public required string Name { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Features { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public SpecificationStatus Status { get; set; } = SpecificationStatus.Draft;
    public required string Owner { get; set; }
    public int Progress { get; set; }
    public required TechStack Stack { get; set; }
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Full rendered markdown for the specification, produced on finalize.</summary>
    public string? SpecMarkdown { get; set; }

    public List<SpecificationPhase> Phases { get; init; } = [];
    public List<Generation> Generations { get; init; } = [];
}
