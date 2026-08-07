using System.Threading.Channels;
using ViAiStudio.AiGenerator.Contracts;

namespace ViAiStudio.AiGenerator.Builds;

public sealed record QueuedBuild(string JobId, StartBuildRequest Request);

/// <summary>
/// Single-worker background queue for build jobs. POST /v1/builds enqueues
/// and returns 202 immediately; this drains the channel one job at a time so
/// a burst of build requests doesn't spawn unbounded background tasks.
/// </summary>
public sealed class BuildQueue
{
    private readonly Channel<QueuedBuild> channel = Channel.CreateUnbounded<QueuedBuild>();

    public void Enqueue(QueuedBuild build) => channel.Writer.TryWrite(build);

    public IAsyncEnumerable<QueuedBuild> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class BuildQueueWorker(BuildQueue queue, IServiceScopeFactory scopeFactory, ILogger<BuildQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var build in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<BuildOrchestrator>();
                await orchestrator.RunAsync(build.JobId, build.Request, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Unhandled error running build job {JobId}", build.JobId);
            }
        }
    }
}
