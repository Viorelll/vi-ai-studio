namespace ViAiStudio.Domain.Entities;

public enum SpecificationGenerationBatchStatus
{
    Pending,
    Running,
    Ready,
    Skipped,
    Failed,
}

/// <summary>
/// One of the ten ordered batches (see the "generation.batch.*" prompt
/// templates) a generation run works through -- _meta, product, architecture,
/// database, backend, frontend, remaining deployables, infrastructure,
/// quality, delivery.
/// </summary>
public sealed class SpecificationGenerationBatch
{
    public Guid Id { get; init; }
    public required Guid RunId { get; init; }

    /// <summary>1-10, matches "generation.batch.{BatchIndex}" in the prompt library.</summary>
    public required int BatchIndex { get; init; }
    public required string Name { get; set; }
    public SpecificationGenerationBatchStatus Status { get; set; } = SpecificationGenerationBatchStatus.Pending;
    public int FilesWritten { get; set; }

    /// <summary>Spec IDs allocated by this batch, so later batches' prompts can say which IDs are already taken.</summary>
    public List<string> AllocatedIds { get; set; } = [];

    public string Note { get; set; } = string.Empty;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
