using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class GenerationRepository(ViAiStudioDbContext dbContext) : IGenerationRepository
{
    public Task AddAsync(Generation generation, CancellationToken cancellationToken)
    {
        dbContext.Generations.Add(generation);
        return Task.CompletedTask;
    }

    public Task<Generation?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Generations.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Generation>> ListBySpecificationAsync(Guid specificationId, CancellationToken cancellationToken) =>
        await dbContext.Generations
            .Where(g => g.SpecificationId == specificationId)
            .OrderByDescending(g => g.Version)
            .ToListAsync(cancellationToken);

    public async Task<int> GetLatestVersionAsync(Guid specificationId, CancellationToken cancellationToken)
    {
        var versions = await dbContext.Generations
            .Where(g => g.SpecificationId == specificationId)
            .Select(g => g.Version)
            .ToListAsync(cancellationToken);
        return versions.Count == 0 ? 0 : versions.Max();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
