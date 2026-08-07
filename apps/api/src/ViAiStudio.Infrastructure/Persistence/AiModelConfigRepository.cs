using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class AiModelConfigRepository(ViAiStudioDbContext dbContext) : IAiModelConfigRepository
{
    public Task AddAsync(AiModelConfig config, CancellationToken cancellationToken)
    {
        dbContext.AiModelConfigs.Add(config);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(AiModelConfig config, CancellationToken cancellationToken)
    {
        dbContext.AiModelConfigs.Remove(config);
        return Task.CompletedTask;
    }

    public Task<AiModelConfig?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AiModelConfigs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AiModelConfig>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.AiModelConfigs.OrderBy(c => c.Label).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
