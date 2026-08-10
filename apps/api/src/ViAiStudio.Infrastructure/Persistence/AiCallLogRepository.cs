using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class AiCallLogRepository(ViAiStudioDbContext dbContext) : IAiCallLogRepository
{
    public Task AddAsync(AiCallLog log, CancellationToken cancellationToken)
    {
        dbContext.AiCallLogs.Add(log);
        return Task.CompletedTask;
    }

    public Task<AiCallLog?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AiCallLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AiCallLog>> ListBySpecificationAsync(
        Guid specificationId, AiCallLogScope scope, CancellationToken cancellationToken) =>
        await ScopedQuery(scope)
            .Where(l => l.SpecificationId == specificationId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AiCallLogRollup>> RollupBySpecificationAsync(
        AiCallLogScope scope, CancellationToken cancellationToken) =>
        await ScopedQuery(scope)
            .GroupBy(l => l.SpecificationId)
            .Select(g => new AiCallLogRollup(g.Key, g.Count(), g.Sum(l => l.Requests), g.Sum(l => (long)l.TokensIn + l.TokensOut)))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Wizard calls carry no <see cref="AiCallLog.GenerationVersion"/>; build
    /// calls are always stamped with the version they ran for. That single
    /// field is what keeps the two Audit surfaces from double-counting.
    /// </summary>
    private IQueryable<AiCallLog> ScopedQuery(AiCallLogScope scope) => scope switch
    {
        AiCallLogScope.Specification => dbContext.AiCallLogs.Where(l => l.GenerationVersion == null),
        AiCallLogScope.Build => dbContext.AiCallLogs.Where(l => l.GenerationVersion != null),
        _ => dbContext.AiCallLogs,
    };

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
