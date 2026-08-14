namespace ViAiStudio.Application.Common;

public sealed record PresignedUpload(string UploadUrl, string StorageKey);

/// <summary>
/// MinIO-backed object storage for generated project archives. AI Generator
/// writes build artifacts directly to the bucket. Browser-facing downloads are
/// streamed back through the Api via <see cref="DownloadAsync"/> rather than
/// redirecting to a presigned URL: those requests carry an Authorization header,
/// which forces a CORS preflight, and a preflighted request may not follow a
/// cross-origin redirect. The presigned helpers remain for server-to-server and
/// direct-upload flows.
/// </summary>
public interface IBlobStorage
{
    Task<PresignedUpload> CreatePresignedUploadAsync(string storageKeyPrefix, string contentType, CancellationToken cancellationToken);
    Task<string> CreatePresignedDownloadAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>Reads an object's full contents into memory (used to peek inside a stored zip archive).</summary>
    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>Writes an object directly (used by the Api itself for artifacts it renders server-side, e.g. specification documents -- unlike generated project archives, which AI Generator writes on its own).</summary>
    Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken);
}
