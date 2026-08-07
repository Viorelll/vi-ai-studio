using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Api.Contracts;

public sealed record GenerationSummaryResponse(
    Guid Id,
    Guid SpecificationId,
    int Version,
    GenerationStatus Status,
    string Note,
    string Model,
    TechStackResponse Stack,
    DateTimeOffset Created,
    int? DurationSeconds)
{
    public static GenerationSummaryResponse FromEntity(Generation generation) => new(
        generation.Id, generation.SpecificationId, generation.Version, generation.Status, generation.Note,
        generation.Model, TechStackResponse.FromValue(generation.Stack), generation.CreatedAt, generation.DurationSeconds);
}

public sealed record GenerationDetailResponse(
    Guid Id,
    Guid SpecificationId,
    int Version,
    GenerationStatus Status,
    string Note,
    string Model,
    TechStackResponse Stack,
    DateTimeOffset Created,
    int? DurationSeconds,
    IReadOnlyList<string> FileTree)
{
    public static GenerationDetailResponse FromEntity(Generation generation) => new(
        generation.Id, generation.SpecificationId, generation.Version, generation.Status, generation.Note,
        generation.Model, TechStackResponse.FromValue(generation.Stack), generation.CreatedAt, generation.DurationSeconds,
        generation.FileTree);
}
