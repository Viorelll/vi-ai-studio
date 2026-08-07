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

    // GetPreSignedUrlRequest.Protocol defaults to HTTPS regardless of the
    // client's UseHttp config, which breaks presigned URLs against a
    // plain-HTTP local MinIO -- so it has to be set explicitly on every request.
    private Protocol DetermineProtocol() =>
        new Uri(options.Value.ServiceUrl).Scheme == Uri.UriSchemeHttp ? Protocol.HTTP : Protocol.HTTPS;
}
