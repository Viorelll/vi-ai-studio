using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

/// <summary>
/// The reusable authoring content the specification wizard is built from --
/// chip groups, interview rounds, authoring rules, the ID scheme, templates
/// and batch instructions -- seeded from PromptLibrarySeedData/ (see
/// SpecificationPromptLibrarySeeder) rather than hardcoded in command handlers.
/// </summary>
public interface ISpecificationPromptLibraryRepository
{
    Task<SpecificationPromptTemplate?> GetAsync(string key, CancellationToken cancellationToken);

    Task<IReadOnlyList<SpecificationPromptTemplate>> ListAsync(
        SpecificationPromptStage stage, string? category, CancellationToken cancellationToken);
}
