using ViAiStudio.Domain.Catalog;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Api.Contracts;

public sealed record TechStackResponse(string Backend, string Ui, string Database, string Infra, string UiStyle)
{
    public static TechStackResponse FromValue(Domain.ValueObjects.TechStack stack) =>
        new(stack.Backend, stack.Ui, stack.Database, stack.Infra, stack.UiStyle);
}

/// <summary>Row shape for the dashboard table, generated-projects list, and landing page.</summary>
public sealed record SpecificationSummaryResponse(
    Guid Id,
    string Name,
    string Summary,
    SpecificationStatus Status,
    string Owner,
    DateTimeOffset Created,
    int Progress,
    TechStackResponse Stack,
    int GenerationCount,
    int? LatestGenerationVersion)
{
    public static SpecificationSummaryResponse FromEntity(Specification specification) => new(
        specification.Id,
        specification.Name,
        specification.Summary,
        specification.Status,
        specification.Owner,
        specification.CreatedAt,
        specification.Progress,
        TechStackResponse.FromValue(specification.Stack),
        specification.Generations.Count,
        specification.Generations.Count == 0 ? null : specification.Generations.Max(g => g.Version));
}

/// <summary>Full detail shape for the specification detail page.</summary>
public sealed record SpecificationDetailResponse(
    Guid Id,
    string Name,
    string Summary,
    string Description,
    string Features,
    string Audience,
    SpecificationStatus Status,
    string Owner,
    DateTimeOffset Created,
    int Progress,
    TechStackResponse Stack,
    string? SpecMarkdown,
    IReadOnlyList<SpecificationPhaseResponse> Phases,
    IReadOnlyList<GenerationSummaryResponse> Generations)
{
    public static SpecificationDetailResponse FromEntity(Specification specification) => new(
        specification.Id,
        specification.Name,
        specification.Summary,
        specification.Description,
        specification.Features,
        specification.Audience,
        specification.Status,
        specification.Owner,
        specification.CreatedAt,
        specification.Progress,
        TechStackResponse.FromValue(specification.Stack),
        specification.SpecMarkdown,
        specification.Phases
            .Where(p => SpecificationPhaseCatalog.Exists(p.PhaseIndex))
            .OrderBy(p => p.PhaseIndex)
            .Select(SpecificationPhaseResponse.FromEntity)
            .ToList(),
        specification.Generations.OrderByDescending(g => g.Version).Select(GenerationSummaryResponse.FromEntity).ToList());
}
