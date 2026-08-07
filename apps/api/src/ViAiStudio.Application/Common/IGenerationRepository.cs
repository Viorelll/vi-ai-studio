using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface IGenerationRepository
{
    Task AddAsync(Generation generation, CancellationToken cancellationToken);
    Task<Generation?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Generation>> ListBySpecificationAsync(Guid specificationId, CancellationToken cancellationToken);

    /// <summary>Highest existing version for a specification, or 0 if it has none yet.</summary>
    Task<int> GetLatestVersionAsync(Guid specificationId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
