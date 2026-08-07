using ViAiStudio.Domain.ValueObjects;

namespace ViAiStudio.Domain.Entities;

public enum GenerationStatus
{
    Running,
    Ready,
    Failed,
}

/// <summary>
/// One AI Build run against a specification. Each run produces the next
/// version of the generated project; <see cref="Version"/> is 1-based and
/// increases per specification, mirroring the "Generated Projects" version
/// timeline.
/// </summary>
public sealed class Generation
{
    public Guid Id { get; init; }
    public required Guid SpecificationId { get; init; }
    public required int Version { get; init; }
    public GenerationStatus Status { get; set; } = GenerationStatus.Running;
    public string Note { get; set; } = string.Empty;

    /// <summary>Label of the <see cref="AiModelConfig"/> used to drive this build.</summary>
    public required string Model { get; set; }

    public required TechStack Stack { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int? DurationSeconds { get; set; }

    /// <summary>Relative paths of every file AI Build produced, in generation order.</summary>
    public List<string> FileTree { get; set; } = [];

    /// <summary>MinIO object key of the zipped project archive, set once the build completes.</summary>
    public string? ArchiveStorageKey { get; set; }
}
