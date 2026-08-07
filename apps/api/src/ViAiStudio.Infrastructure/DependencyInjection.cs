using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViAiStudio.Application.Common;
using ViAiStudio.Infrastructure.AiGenerator;
using ViAiStudio.Infrastructure.Health;
using ViAiStudio.Infrastructure.Persistence;
using ViAiStudio.Infrastructure.Storage;

namespace ViAiStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ViAiStudioDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<ISpecificationRepository, SpecificationRepository>();
        services.AddScoped<IGenerationRepository, GenerationRepository>();
        services.AddScoped<IAiModelConfigRepository, AiModelConfigRepository>();
        services.AddScoped<ITaskRoutingRepository, TaskRoutingRepository>();
        services.AddScoped<IAiCallLogRepository, AiCallLogRepository>();

        services.Configure<MinioStorageOptions>(configuration.GetSection("Storage"));
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinioStorageOptions>>().Value;
            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = true, // MinIO doesn't support virtual-hosted-style bucket URLs.
                UseHttp = new Uri(options.ServiceUrl).Scheme == Uri.UriSchemeHttp,
            };
            return new AmazonS3Client(credentials, config);
        });
        services.AddScoped<IBlobStorage, MinioStorageClient>();

        var aiGeneratorBaseUrl = configuration["AiGenerator:BaseUrl"]!;
        services.AddHttpClient<IAiGeneratorClient, AiGeneratorHttpClient>(client =>
        {
            client.BaseAddress = new Uri(aiGeneratorBaseUrl);
        });
        services.AddHttpClient<IServiceHealthChecker, ServiceHealthChecker>(client =>
        {
            client.BaseAddress = new Uri(aiGeneratorBaseUrl);
        });

        services.AddSingleton<IBuildEventBroadcaster, BuildEventBroadcaster>();

        return services;
    }
}
