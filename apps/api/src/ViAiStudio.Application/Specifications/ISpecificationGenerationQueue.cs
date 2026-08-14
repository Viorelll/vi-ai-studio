namespace ViAiStudio.Application.Specifications;

/// <summary>
/// Single-worker background queue for stage-3 generation runs. Starting a
/// run enqueues and returns immediately; a background worker drains this one
/// run at a time so a burst of "start generation" clicks doesn't spawn
/// unbounded background tasks. Mirrors AI Generator's own BuildQueue.
/// </summary>
public interface ISpecificationGenerationQueue
{
    void Enqueue(Guid runId);

    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}
