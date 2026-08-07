using Amazon.S3;
using Microsoft.Extensions.Configuration;
using ViAiStudio.Application.Common;

namespace ViAiStudio.Infrastructure.Health;

/// <summary>Backs the Admin dashboard's service status badges by probing each peer directly.</summary>
public sealed class ServiceHealthChecker(HttpClient aiGeneratorHttpClient, IAmazonS3 s3Client, IConfiguration configuration) : IServiceHealthChecker
{
    public async Task<IReadOnlyList<ServiceHealth>> CheckAllAsync(CancellationToken cancellationToken)
    {
        var aiGeneratorOnline = await PingAsync(async () =>
            (await aiGeneratorHttpClient.GetAsync("/health", cancellationToken)).EnsureSuccessStatusCode());
        var storageOnline = await PingAsync(() => s3Client.ListBucketsAsync(cancellationToken));

        return
        [
            new ServiceHealth("API", true, configuration["Api:PublicBaseUrl"] ?? "http://localhost:5080",
                "Core REST API powering specifications, builds, and task routing."),
            new ServiceHealth("AI Generator", aiGeneratorOnline, aiGeneratorHttpClient.BaseAddress?.ToString() ?? "",
                "Model orchestration service used by AI Specification Studio and AI Build."),
            new ServiceHealth("Storage (MinIO)", storageOnline, configuration["Storage:ServiceUrl"] ?? "",
                "S3-compatible object storage for generated project artifacts and build logs."),
        ];
    }

    private static async Task<bool> PingAsync(Func<Task> probe)
    {
        try
        {
            await probe();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
