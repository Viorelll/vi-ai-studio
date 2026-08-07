using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace ViAiStudio.AiGenerator.Storage;

public sealed class MinioArchiveOptions
{
    public required string ServiceUrl { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required string BucketName { get; set; }
}

/// <summary>Writes a build's generated project archive to the same MinIO bucket the Api hands out presigned downloads for.</summary>
public sealed class MinioArchiveWriter(IAmazonS3 s3Client, IOptions<MinioArchiveOptions> options)
{
    public async Task<string> WriteArchiveAsync(Guid generationId, byte[] zipBytes, CancellationToken cancellationToken)
    {
        var storageKey = $"builds/{generationId}/project.zip";

        using var stream = new MemoryStream(zipBytes);
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = storageKey,
            InputStream = stream,
            ContentType = "application/zip",
        }, cancellationToken);

        return storageKey;
    }
}
