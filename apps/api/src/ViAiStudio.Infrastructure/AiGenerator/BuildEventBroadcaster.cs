using System.Collections.Concurrent;
using System.Threading.Channels;
using ViAiStudio.Application.Common;

namespace ViAiStudio.Infrastructure.AiGenerator;

/// <summary>
/// In-process pub/sub for live build logs. One channel per generation, torn
/// down once every subscriber has disconnected or the build completes -- a
/// second Api replica would need Redis pub/sub instead, but a single
/// instance is enough for this stage.
/// </summary>
public sealed class BuildEventBroadcaster : IBuildEventBroadcaster
{
    private readonly ConcurrentDictionary<Guid, List<Channel<BuildEvent>>> subscribers = new();

    public void Publish(BuildEvent buildEvent)
    {
        if (!subscribers.TryGetValue(buildEvent.GenerationId, out var channels))
        {
            return;
        }

        lock (channels)
        {
            foreach (var channel in channels)
            {
                channel.Writer.TryWrite(buildEvent);
                if (buildEvent.Done)
                {
                    channel.Writer.TryComplete();
                }
            }

            if (buildEvent.Done)
            {
                subscribers.TryRemove(buildEvent.GenerationId, out _);
            }
        }
    }

    public async IAsyncEnumerable<BuildEvent> SubscribeAsync(
        Guid generationId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<BuildEvent>();
        var channels = subscribers.GetOrAdd(generationId, _ => []);
        lock (channels)
        {
            channels.Add(channel);
        }

        try
        {
            await foreach (var buildEvent in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return buildEvent;
            }
        }
        finally
        {
            lock (channels)
            {
                channels.Remove(channel);
            }
        }
    }
}
