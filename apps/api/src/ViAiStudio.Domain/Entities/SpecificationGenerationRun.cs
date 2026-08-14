namespace ViAiStudio.Domain.Entities;

public enum SpecificationGenerationRunStatus
{
    Pending,
    Running,
    Ready,
    Failed,
}

/// <summary>
/// One stage-3 batch-generation run against a specification's intake. Mirrors
/// <see cref="Generation"/> (AI Build) in shape, but produces specification
/// documents instead of a working project.
/// </summary>
public sealed class SpecificationGenerationRun
{
    public Guid Id { get; init; }
    public required Guid SpecificationId { get; init; }
    public SpecificationGenerationRunStatus Status { get; set; } = SpecificationGenerationRunStatus.Pending;
    public required string Model { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int? DurationSeconds { get; set; }

    public List<SpecificationGenerationBatch> Batches { get; init; } = [];
}
