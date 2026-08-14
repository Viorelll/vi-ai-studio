using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ViAiStudio.Application.Specifications;

namespace ViAiStudio.Infrastructure.SpecGeneration;

public sealed class SpecificationGenerationQueue : ISpecificationGenerationQueue
{
    private readonly Channel<Guid> channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid runId) => channel.Writer.TryWrite(runId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class SpecificationGenerationQueueWorker(
    ISpecificationGenerationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<SpecificationGenerationQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var runId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<RunSpecificationGenerationHandler>();
                await handler.HandleAsync(runId, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Unhandled error running specification generation {RunId}", runId);
            }
        }
    }
}
