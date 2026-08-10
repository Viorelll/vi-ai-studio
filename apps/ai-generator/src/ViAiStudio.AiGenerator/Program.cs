using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using ViAiStudio.AiGenerator.Builds;
using ViAiStudio.AiGenerator.Callback;
using ViAiStudio.AiGenerator.Endpoints;
using ViAiStudio.AiGenerator.Generation;
using ViAiStudio.AiGenerator.Providers;
using ViAiStudio.AiGenerator.Sandbox;
using ViAiStudio.AiGenerator.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddHttpClient<IModelProvider, AzureFoundryModelProvider>(client =>
{
    // Generating or repairing an entire repository is one long-running request;
    // HttpClient's 100-second default aborts it mid-flight.
    client.Timeout = TimeSpan.FromMinutes(15);
});
builder.Services.AddHttpClient<ApiCallbackClient>();

builder.Services.Configure<MinioArchiveOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var options = sp.GetRequiredService<IOptions<MinioArchiveOptions>>().Value;
    var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
    var config = new AmazonS3Config
    {
        ServiceURL = options.ServiceUrl,
        ForcePathStyle = true, // MinIO doesn't support virtual-hosted-style bucket URLs.
        UseHttp = new Uri(options.ServiceUrl).Scheme == Uri.UriSchemeHttp,
    };
    return new AmazonS3Client(credentials, config);
});
builder.Services.AddSingleton<MinioArchiveWriter>();

builder.Services.Configure<SandboxOptions>(builder.Configuration.GetSection("Sandbox"));
builder.Services.AddSingleton<ISandboxExecutor, DockerSandboxExecutor>();
builder.Services.AddSingleton<ProjectVerifier>();
builder.Services.AddSingleton<BuildWorkspaceFactory>();
builder.Services.AddSingleton<ProjectCodeGenerator>();

builder.Services.AddSingleton<IBuildJobStore, InMemoryBuildJobStore>();
builder.Services.AddSingleton<BuildQueue>();
builder.Services.AddHostedService<BuildQueueWorker>();
builder.Services.AddScoped<BuildOrchestrator>();

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => Results.Redirect("/scalar"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGenerateEndpoints();
app.MapBuildsEndpoints();

app.Run();
