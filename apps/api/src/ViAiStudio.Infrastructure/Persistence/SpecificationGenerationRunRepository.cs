using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class SpecificationGenerationRunRepository(ViAiStudioDbContext dbContext) : ISpecificationGenerationRunRepository
{
    public Task<SpecificationGenerationRun?> GetAsync(Guid runId, CancellationToken cancellationToken) =>
        dbContext.SpecificationGenerationRuns
            .Include(r => r.Batches)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

    public async Task<IReadOnlyList<SpecificationGenerationRun>> ListAsync(Guid specificationId, CancellationToken cancellationToken) =>
        await dbContext.SpecificationGenerationRuns
            .Include(r => r.Batches)
            .Where(r => r.SpecificationId == specificationId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(SpecificationGenerationRun run, CancellationToken cancellationToken)
    {
        dbContext.SpecificationGenerationRuns.Add(run);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
