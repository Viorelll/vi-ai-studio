using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViAiStudio.Application.Common;
using ViAiStudio.Application.Specifications;
using ViAiStudio.Infrastructure.AiGenerator;
using ViAiStudio.Infrastructure.Auth;
using ViAiStudio.Infrastructure.Persistence;
using ViAiStudio.Infrastructure.SpecGeneration;
using ViAiStudio.Infrastructure.Storage;

namespace ViAiStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ViAiStudioDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<ISpecificationRepository, SpecificationRepository>();
        services.AddScoped<ISpecificationPromptLibraryRepository, SpecificationPromptLibraryRepository>();
        services.AddScoped<ISpecificationDocumentRepository, SpecificationDocumentRepository>();
        services.AddScoped<ISpecificationGenerationRunRepository, SpecificationGenerationRunRepository>();
        services.AddScoped<ISpecificationValidationIssueRepository, SpecificationValidationIssueRepository>();
        services.AddSingleton<ISpecificationGenerationQueue, SpecificationGenerationQueue>();
        services.AddHostedService<SpecificationGenerationQueueWorker>();
        services.AddScoped<IGenerationRepository, GenerationRepository>();
        services.AddScoped<IAiModelConfigRepository, AiModelConfigRepository>();
        services.AddScoped<ITaskRoutingRepository, TaskRoutingRepository>();
        services.AddScoped<IAiCallLogRepository, AiCallLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.Configure<GoogleOptions>(configuration.GetSection("Auth:Google"));
        services.Configure<JwtOptions>(configuration.GetSection("Auth:Jwt"));
        services.AddScoped<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

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
            // Default HttpClient.Timeout is 100s -- too short for a single spec
            // generation batch, which can legitimately run several minutes for a
            // dense batch (e.g. backend + a dozen endpoint files) at high token counts.
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddSingleton<IBuildEventBroadcaster, BuildEventBroadcaster>();

        return services;
    }
}
