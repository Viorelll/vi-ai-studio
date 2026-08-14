using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using ViAiStudio.Application.Common;

namespace ViAiStudio.Infrastructure.Storage;

public sealed class MinioStorageOptions
{
    public required string ServiceUrl { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required string BucketName { get; set; }
    public TimeSpan UploadUrlExpiry { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan DownloadUrlExpiry { get; set; } = TimeSpan.FromMinutes(15);
}

public sealed class MinioStorageClient(IAmazonS3 s3Client, IOptions<MinioStorageOptions> options) : IBlobStorage
{
    public Task<PresignedUpload> CreatePresignedUploadAsync(string storageKeyPrefix, string contentType, CancellationToken cancellationToken)
    {
        var storageKey = $"{storageKeyPrefix.TrimEnd('/')}/{Guid.NewGuid()}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = options.Value.BucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Protocol = DetermineProtocol(),
            Expires = DateTime.UtcNow.Add(options.Value.UploadUrlExpiry),
        };

        var uploadUrl = s3Client.GetPreSignedURL(request);
        return Task.FromResult(new PresignedUpload(uploadUrl, storageKey));
    }

    public Task<string> CreatePresignedDownloadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = options.Value.BucketName,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Protocol = DetermineProtocol(),
            Expires = DateTime.UtcNow.Add(options.Value.DownloadUrlExpiry),
        };

        return Task.FromResult(s3Client.GetPreSignedURL(request));
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken)
    {
        using var response = await s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = storageKey,
        }, cancellationToken);

        // Buffered into memory rather than handed back as the raw response stream --
        // ZipArchive needs random (seekable) access to read a zip's central directory,
        // which S3's network response stream doesn't support.
        var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public async Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
        }, cancellationToken);
    }

    // GetPreSignedUrlRequest.Protocol defaults to HTTPS regardless of the
    // client's UseHttp config, which breaks presigned URLs against a
    // plain-HTTP local MinIO -- so it has to be set explicitly on every request.
    private Protocol DetermineProtocol() =>
        new Uri(options.Value.ServiceUrl).Scheme == Uri.UriSchemeHttp ? Protocol.HTTP : Protocol.HTTPS;
}
