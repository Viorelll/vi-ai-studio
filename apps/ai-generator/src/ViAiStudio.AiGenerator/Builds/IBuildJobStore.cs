namespace ViAiStudio.AiGenerator.Builds;

/// <summary>
/// In-memory job status, used only as a polling fallback (GET /v1/builds/{jobId})
/// for a client that missed events on the Api's SSE stream. Not durable --
/// a restart loses in-flight jobs, same as the rest of this stateless service.
/// </summary>
public interface IBuildJobStore
{
    BuildJob Create(Guid generationId);
    BuildJob? Get(string jobId);
    void Update(string jobId, Action<BuildJob> mutate);
}
