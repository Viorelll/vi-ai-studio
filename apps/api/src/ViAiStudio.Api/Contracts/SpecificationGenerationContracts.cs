using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Api.Contracts;

public sealed record SpecificationGenerationBatchResponse(
    int BatchIndex, string Name, SpecificationGenerationBatchStatus Status, int FilesWritten, string Note)
{
    public static SpecificationGenerationBatchResponse FromEntity(SpecificationGenerationBatch batch) =>
        new(batch.BatchIndex, batch.Name, batch.Status, batch.FilesWritten, batch.Note);
}

public sealed record SpecificationGenerationRunResponse(
    Guid Id,
    SpecificationGenerationRunStatus Status,
    string Model,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int? DurationSeconds,
    IReadOnlyList<SpecificationGenerationBatchResponse> Batches)
{
    public static SpecificationGenerationRunResponse FromEntity(SpecificationGenerationRun run) => new(
        run.Id, run.Status, run.Model, run.CreatedAt, run.StartedAt, run.CompletedAt, run.DurationSeconds,
        run.Batches.OrderBy(b => b.BatchIndex).Select(SpecificationGenerationBatchResponse.FromEntity).ToList());
}

public sealed record ValidationIssueResponse(ValidationIssueSeverity Severity, string Code, string Message, string? DocumentPath)
{
    public static ValidationIssueResponse FromEntity(SpecificationValidationIssue issue) =>
        new(issue.Severity, issue.Code, issue.Message, issue.DocumentPath);
}
