using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface IAiCallLogRepository
{
    Task AddAsync(AiCallLog log, CancellationToken cancellationToken);
    Task<AiCallLog?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiCallLog>> ListBySpecificationAsync(Guid specificationId, CancellationToken cancellationToken);

    /// <summary>Per-specification rollups (log count, total requests, total tokens) for the Audit list.</summary>
    Task<IReadOnlyList<AiCallLogRollup>> RollupBySpecificationAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record AiCallLogRollup(Guid SpecificationId, int LogCount, int TotalRequests, long TotalTokens);
