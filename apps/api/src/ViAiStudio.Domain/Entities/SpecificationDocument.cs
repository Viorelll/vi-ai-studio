namespace ViAiStudio.Domain.Entities;

/// <summary>
/// One generated specification file -- a first-class row rather than text
/// derived on the fly, so the manifest and validation (duplicate IDs,
/// dangling dependencies, cycles) can query front-matter directly instead of
/// re-parsing markdown. <see cref="Content"/> is the full file including its
/// rendered YAML front-matter block, so it round-trips byte-for-byte into the
/// zip archive and the download.
/// </summary>
public sealed class SpecificationDocument
{
    public Guid Id { get; init; }
    public required Guid SpecificationId { get; init; }
    public Guid? BatchId { get; set; }

    /// <summary>Repository-relative path, e.g. "02-database/03-entities/03-projects.md". Unique per specification.</summary>
    public required string Path { get; set; }

    /// <summary>Front-matter "id", e.g. "DB-013". Allocated per the ID scheme (see the "generation.id-scheme" prompt template).</summary>
    public required string SpecId { get; set; }
    public required string Title { get; set; }
    public required string Component { get; set; }
    public string Status { get; set; } = "draft";
    public string Version { get; set; } = "1.0";
    public List<string> DependsOn { get; set; } = [];
    public List<string> Provides { get; set; } = [];
    public List<string> Generates { get; set; } = [];

    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
