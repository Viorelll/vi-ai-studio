using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class SpecificationValidationIssueRepository(ViAiStudioDbContext dbContext) : ISpecificationValidationIssueRepository
{
    public async Task<IReadOnlyList<SpecificationValidationIssue>> ListAsync(Guid specificationId, CancellationToken cancellationToken) =>
        await dbContext.SpecificationValidationIssues
            .Where(i => i.SpecificationId == specificationId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
}
