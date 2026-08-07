namespace ViAiStudio.Application.Common;

public sealed record PresignedUpload(string UploadUrl, string StorageKey);

/// <summary>
/// MinIO-backed object storage for generated project archives. AI Generator
/// writes build artifacts directly to the bucket; the Api only ever hands
/// out presigned URLs so browsers upload/download straight to/from MinIO.
/// </summary>
public interface IBlobStorage
{
    Task<PresignedUpload> CreatePresignedUploadAsync(string storageKeyPrefix, string contentType, CancellationToken cancellationToken);
    Task<string> CreatePresignedDownloadAsync(string storageKey, CancellationToken cancellationToken);
}
