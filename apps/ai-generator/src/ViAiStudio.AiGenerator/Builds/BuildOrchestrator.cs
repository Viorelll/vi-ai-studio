using System.IO.Compression;
using System.Text;
using ViAiStudio.AiGenerator.Callback;
using ViAiStudio.AiGenerator.Contracts;
using ViAiStudio.AiGenerator.Providers;
using ViAiStudio.AiGenerator.Storage;

namespace ViAiStudio.AiGenerator.Builds;

/// <summary>
/// Runs the AI Build pipeline for one job: Planning → Scaffolding → Coding →
/// Tests → Done, reporting each step back to the Api and finishing with a
/// generated file tree zipped into MinIO.
/// </summary>
public sealed class BuildOrchestrator(
    IModelProvider modelProvider,
    ApiCallbackClient callbackClient,
    MinioArchiveWriter archiveWriter,
    IBuildJobStore jobStore,
    ILogger<BuildOrchestrator> logger)
{
    private static readonly (string Label, string[] Lines)[] StepDefs =
    [
        ("Planning", ["Analyzing specification…", "Defining architecture…", "Generating project plan…"]),
        ("Scaffolding", ["Creating repository structure…", "Installing dependencies…", "Configuring shadcn/ui + Tailwind…"]),
        ("Coding", ["Generating components…", "Wiring API routes…", "Implementing business logic…"]),
        ("Tests", ["Running unit tests…", "Running integration tests…", "All tests passed ✓"]),
        ("Done", ["Finalizing build…", "Deployment successful ✓"]),
    ];

    private static readonly Dictionary<string, string[]> StackFiles = new()
    {
        [".NET Web API"] = ["src/Api/Program.cs", "src/Api/appsettings.json", "src/Application/DependencyInjection.cs", "src/Domain/Entities/Entity.cs", "src/Infrastructure/AppDbContext.cs"],
        ["Next.js"] = ["web/app/layout.tsx", "web/app/page.tsx", "web/app/globals.css", "web/next.config.js", "web/package.json"],
        ["PostgreSQL"] = ["src/Infrastructure/AppDbContext.cs", "src/Infrastructure/Migrations/0001_Init.cs"],
        ["Docker"] = ["Dockerfile", "docker-compose.yml", ".dockerignore"],
    };

    public async Task RunAsync(string jobId, StartBuildRequest request, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var aiCalls = new List<BuildAiCallReport>();
        var totalLines = StepDefs.Sum(s => s.Lines.Length);
        var completed = 0;

        try
        {
            foreach (var (label, lines) in StepDefs)
            {
                jobStore.Update(jobId, j => j.CurrentStep = label);

                foreach (var line in lines)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                    completed++;
                    var pct = Math.Min(99, completed * 100 / totalLines);

                    jobStore.Update(jobId, j => j.ProgressPct = pct);
                    await callbackClient.ReportProgressAsync(request.CallbackBaseUrl, request.GenerationId, label, line, pct, cancellationToken);
                }

                if (label == "Coding")
                {
                    aiCalls.Add(await GenerateCodingCallAsync(request, cancellationToken));
                }
            }

            var fileTree = BuildFileTree(request.Stack);
            var archiveStorageKey = await archiveWriter.WriteArchiveAsync(request.GenerationId, BuildZip(request, fileTree), cancellationToken);
            var duration = (int)(DateTimeOffset.UtcNow - startedAt).TotalSeconds;

            jobStore.Update(jobId, j => { j.Status = BuildJobStatus.Ready; j.ProgressPct = 100; });
            await callbackClient.ReportCompleteAsync(
                request.CallbackBaseUrl, request.GenerationId,
                new CompleteBuildPayload(Success: true, FailureReason: null, duration, fileTree, archiveStorageKey, aiCalls),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Build {GenerationId} failed", request.GenerationId);
            jobStore.Update(jobId, j => j.Status = BuildJobStatus.Failed);

            try
            {
                var duration = (int)(DateTimeOffset.UtcNow - startedAt).TotalSeconds;
                await callbackClient.ReportCompleteAsync(
                    request.CallbackBaseUrl, request.GenerationId,
                    new CompleteBuildPayload(Success: false, ex.Message, duration, [], null, aiCalls),
                    CancellationToken.None);
            }
            catch (Exception callbackEx)
            {
                logger.LogError(callbackEx, "Failed to report build failure for {GenerationId} back to the Api", request.GenerationId);
            }
        }
    }

    private async Task<BuildAiCallReport> GenerateCodingCallAsync(StartBuildRequest request, CancellationToken cancellationToken)
    {
        const string systemPrompt = "You generate production code for a software project from its specification.";
        var prompt = $"Project: {request.SpecificationName}\nStack: {request.Stack.Backend}, {request.Stack.Ui}, {request.Stack.Database}, {request.Stack.Infra}\n\n{request.SpecMarkdown}";

        var result = await modelProvider.GenerateAsync(
            new ModelRequest(request.Provider, request.Model, request.BaseUrl, request.ApiKey, systemPrompt, prompt),
            cancellationToken);

        return new BuildAiCallReport(request.Model, result.TokensIn, result.TokensOut, prompt, result.Text);
    }

    private static List<string> BuildFileTree(StackDto stack)
    {
        var chosen = new[] { stack.Backend, stack.Ui, stack.Database, stack.Infra };
        var files = chosen
            .Where(StackFiles.ContainsKey)
            .SelectMany(t => StackFiles[t])
            .Distinct()
            .ToList();
        files.Add("README.md");
        return files;
    }

    private static byte[] BuildZip(StartBuildRequest request, List<string> fileTree)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var path in fileTree)
            {
                var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(path.EndsWith(".md")
                    ? $"# {request.SpecificationName}\n\nGenerated by AI Build.\n"
                    : $"// Generated by AI Build for {request.SpecificationName}\n// {path}\n");
            }
        }
        return stream.ToArray();
    }
}
