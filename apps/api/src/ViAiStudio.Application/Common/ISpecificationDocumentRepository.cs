using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

/// <summary>Lightweight projection of a generated document for browsing -- omits Content, which can be large across ~100 documents.</summary>
public sealed record SpecificationDocumentSummary(
    string Path, string SpecId, string Title, string Component, string Status, string Version,
    IReadOnlyList<string> DependsOn, IReadOnlyList<string> Provides, IReadOnlyList<string> Generates);

public interface ISpecificationDocumentRepository
{
    /// <summary>Path + front-matter only, no Content -- backs the file tree and the manifest/validation passes.</summary>
    Task<IReadOnlyList<SpecificationDocumentSummary>> ListAsync(Guid specificationId, CancellationToken cancellationToken);

    /// <summary>One document, full content -- backs the file preview and the zip.</summary>
    Task<SpecificationDocument?> GetAsync(Guid specificationId, string path, CancellationToken cancellationToken);

    /// <summary>Every document, full content -- backs the MinIO zip sync.</summary>
    Task<IReadOnlyList<SpecificationDocument>> ListAllAsync(Guid specificationId, CancellationToken cancellationToken);

    /// <summary>Inserts a new document or overwrites an existing one at the same path.</summary>
    Task UpsertAsync(SpecificationDocument document, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
