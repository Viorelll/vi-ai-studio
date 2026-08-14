namespace ViAiStudio.Domain.Entities;

public enum ValidationIssueSeverity
{
    Warning,
    Error,
}

/// <summary>
/// A problem found in a specification's generated documents (duplicate ID,
/// dangling dependency, dependency cycle, path collision, missing acceptance
/// criteria). Non-blocking: a draft spec with listed problems is more useful
/// than one that silently invented an answer.
/// </summary>
public sealed class SpecificationValidationIssue
{
    public Guid Id { get; init; }
    public required Guid SpecificationId { get; init; }
    public Guid? RunId { get; set; }
    public ValidationIssueSeverity Severity { get; set; } = ValidationIssueSeverity.Warning;

    /// <summary>"duplicate-id" | "dangling-depends-on" | "dependency-cycle" | "missing-acceptance-criteria" | "path-collision".</summary>
    public required string Code { get; set; }
    public required string Message { get; set; }
    public string? DocumentPath { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
