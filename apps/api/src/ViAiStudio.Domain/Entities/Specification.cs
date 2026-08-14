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
/// A project specification authored through the wizard's three-stage
/// pipeline: chip selection, domain interview, batch generation. Starts as
/// Draft, moves to Building while stage 3 runs, and becomes Ready once
/// generation completes and validation has run. This status is the
/// specification's own -- it never changes because of what any one
/// <see cref="Generation"/> (AI Build run) does; a specification can be
/// rebuilt many times, successfully or not, without its own Ready status
/// moving. Build-specific progress lives on each <see cref="Generation"/>
/// instead.
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

    /// <summary>Relative paths of every document the spec currently renders to, mirrors <see cref="Generation.FileTree"/>. Refreshed each time the documents are synced to MinIO.</summary>
    public List<string> DocumentPaths { get; set; } = [];

    /// <summary>MinIO object key of the zipped documents archive, refreshed whenever the documents are synced.</summary>
    public string? DocumentsArchiveStorageKey { get; set; }

    public List<Generation> Generations { get; init; } = [];

    /// <summary>Stage 1 (chip selection) decisions. Null until the first chip save.</summary>
    public SpecificationIntakeSheet? Intake { get; set; }

    /// <summary>Stage 2 (domain interview) answers.</summary>
    public List<SpecificationInterviewAnswer> InterviewAnswers { get; init; } = [];

    /// <summary>Stage 3 (generation) output -- the real, persisted specification files.</summary>
    public List<SpecificationDocument> Documents { get; init; } = [];

    /// <summary>Stage 3 batch-generation runs.</summary>
    public List<SpecificationGenerationRun> GenerationRuns { get; init; } = [];
}
