using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class SpecificationRepository(ViAiStudioDbContext dbContext) : ISpecificationRepository
{
    public Task AddAsync(Specification specification, CancellationToken cancellationToken)
    {
        dbContext.Specifications.Add(specification);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Specification specification, CancellationToken cancellationToken)
    {
        dbContext.Specifications.Remove(specification);
        return Task.CompletedTask;
    }

    public Task<Specification?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Specifications
            .Include(s => s.Phases)
            .Include(s => s.Generations)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Specifications.AnyAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Specification>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Specifications
            .Include(s => s.Generations)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
