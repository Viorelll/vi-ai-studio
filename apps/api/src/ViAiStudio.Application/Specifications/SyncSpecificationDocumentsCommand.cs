using System.IO.Compression;
using System.Text;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record SpecificationDocumentsSyncResult(IReadOnlyList<SpecificationDocument> Documents, string StorageKey);

/// <summary>
/// Zips a specification's persisted documents and uploads them to MinIO,
/// caching the resulting path list and storage key on the entity. Called
/// once per finished stage-3 batch (see RunSpecificationGenerationHandler) so
/// the archive and the cached "N files" count are never stale relative to
/// what's actually been written, without rebuilding on every read.
/// </summary>
public sealed class SyncSpecificationDocumentsHandler(
    ISpecificationRepository specificationRepository,
    ISpecificationDocumentRepository documentRepository,
    IBlobStorage blobStorage)
{
    public async Task<SpecificationDocumentsSyncResult> HandleAsync(Specification specification, CancellationToken cancellationToken)
    {
        var documents = await documentRepository.ListAllAsync(specification.Id, cancellationToken);

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var document in documents)
            {
                var entry = zip.CreateEntry(document.Path, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(document.Content);
            }
        }
        stream.Position = 0;

        var storageKey = $"specifications/{specification.Id}/documents.zip";
        await blobStorage.UploadAsync(storageKey, stream, "application/zip", cancellationToken);

        specification.DocumentPaths = documents.Select(d => d.Path).ToList();
        specification.DocumentsArchiveStorageKey = storageKey;
        await specificationRepository.SaveChangesAsync(cancellationToken);

        return new SpecificationDocumentsSyncResult(documents, storageKey);
    }
}
