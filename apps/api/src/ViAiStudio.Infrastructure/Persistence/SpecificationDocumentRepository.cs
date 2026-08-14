using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class SpecificationDocumentRepository(ViAiStudioDbContext dbContext) : ISpecificationDocumentRepository
{
    public async Task<IReadOnlyList<SpecificationDocumentSummary>> ListAsync(Guid specificationId, CancellationToken cancellationToken) =>
        await dbContext.SpecificationDocuments
            .Where(d => d.SpecificationId == specificationId)
            .OrderBy(d => d.Path)
            .Select(d => new SpecificationDocumentSummary(
                d.Path, d.SpecId, d.Title, d.Component, d.Status, d.Version, d.DependsOn, d.Provides, d.Generates))
            .ToListAsync(cancellationToken);

    public Task<SpecificationDocument?> GetAsync(Guid specificationId, string path, CancellationToken cancellationToken) =>
        dbContext.SpecificationDocuments
            .FirstOrDefaultAsync(d => d.SpecificationId == specificationId && d.Path == path, cancellationToken);

    public async Task<IReadOnlyList<SpecificationDocument>> ListAllAsync(Guid specificationId, CancellationToken cancellationToken) =>
        await dbContext.SpecificationDocuments
            .Where(d => d.SpecificationId == specificationId)
            .OrderBy(d => d.Path)
            .ToListAsync(cancellationToken);

    public async Task UpsertAsync(SpecificationDocument document, CancellationToken cancellationToken)
    {
        var existing = await dbContext.SpecificationDocuments.FirstOrDefaultAsync(
            d => d.SpecificationId == document.SpecificationId && d.Path == document.Path, cancellationToken);

        if (existing is null)
        {
            document.CreatedAt = DateTimeOffset.UtcNow;
            document.UpdatedAt = document.CreatedAt;
            dbContext.SpecificationDocuments.Add(document);
            return;
        }

        existing.BatchId = document.BatchId;
        existing.SpecId = document.SpecId;
        existing.Title = document.Title;
        existing.Component = document.Component;
        existing.Status = document.Status;
        existing.Version = document.Version;
        existing.DependsOn = document.DependsOn;
        existing.Provides = document.Provides;
        existing.Generates = document.Generates;
        existing.Content = document.Content;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
