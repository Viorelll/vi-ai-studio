using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class SpecificationPromptLibraryRepository(ViAiStudioDbContext dbContext) : ISpecificationPromptLibraryRepository
{
    public Task<SpecificationPromptTemplate?> GetAsync(string key, CancellationToken cancellationToken) =>
        dbContext.SpecificationPromptTemplates
            .FirstOrDefaultAsync(t => t.Key == key && t.IsActive, cancellationToken);

    public async Task<IReadOnlyList<SpecificationPromptTemplate>> ListAsync(
        SpecificationPromptStage stage, string? category, CancellationToken cancellationToken)
    {
        var query = dbContext.SpecificationPromptTemplates.Where(t => t.Stage == stage && t.IsActive);
        if (category is not null)
        {
            query = query.Where(t => t.Category == category);
        }
        return await query.OrderBy(t => t.OrderIndex).ToListAsync(cancellationToken);
    }
}
