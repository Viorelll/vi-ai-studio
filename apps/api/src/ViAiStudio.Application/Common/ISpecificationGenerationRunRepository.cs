using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface ISpecificationGenerationRunRepository
{
    /// <summary>Loads a run with its batches.</summary>
    Task<SpecificationGenerationRun?> GetAsync(Guid runId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SpecificationGenerationRun>> ListAsync(Guid specificationId, CancellationToken cancellationToken);

    /// <summary>Adds a brand-new run (with its Batches already populated) -- the whole graph is tracked as Added together.</summary>
    Task AddAsync(SpecificationGenerationRun run, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
