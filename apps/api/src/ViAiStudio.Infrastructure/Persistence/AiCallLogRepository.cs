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

    public async Task<IReadOnlyList<AiCallLog>> ListBySpecificationAsync(Guid specificationId, CancellationToken cancellationToken) =>
        await dbContext.AiCallLogs
            .Where(l => l.SpecificationId == specificationId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AiCallLogRollup>> RollupBySpecificationAsync(CancellationToken cancellationToken) =>
        await dbContext.AiCallLogs
            .GroupBy(l => l.SpecificationId)
            .Select(g => new AiCallLogRollup(g.Key, g.Count(), g.Sum(l => l.Requests), g.Sum(l => (long)l.TokensIn + l.TokensOut)))
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
