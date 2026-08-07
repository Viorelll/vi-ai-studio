namespace ViAiStudio.Application.Common;

public sealed record BuildEvent(Guid GenerationId, string StepLabel, string LogLine, int ProgressPct, bool Done);

/// <summary>
/// Fans out AI Build progress events (received from AI Generator's webhook
/// callbacks) to any client watching the build's live log via SSE. Events
/// are ephemeral -- only the final Generation state is persisted.
/// </summary>
public interface IBuildEventBroadcaster
{
    void Publish(BuildEvent buildEvent);

    /// <summary>Subscribes to events for one generation until the returned reader's channel completes.</summary>
    IAsyncEnumerable<BuildEvent> SubscribeAsync(Guid generationId, CancellationToken cancellationToken);
}
