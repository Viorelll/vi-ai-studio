using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface ISpecificationRepository
{
    Task AddAsync(Specification specification, CancellationToken cancellationToken);
    Task RemoveAsync(Specification specification, CancellationToken cancellationToken);

    /// <summary>Loads a specification with its phases and generations.</summary>
    Task<Specification?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Specification>> ListAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
