using System.Collections.Concurrent;

namespace ViAiStudio.AiGenerator.Builds;

public sealed class InMemoryBuildJobStore : IBuildJobStore
{
    private readonly ConcurrentDictionary<string, BuildJob> jobs = new();

    public BuildJob Create(Guid generationId)
    {
        var job = new BuildJob { JobId = Guid.NewGuid().ToString("n"), GenerationId = generationId };
        jobs[job.JobId] = job;
        return job;
    }

    public BuildJob? Get(string jobId) => jobs.GetValueOrDefault(jobId);

    public void Update(string jobId, Action<BuildJob> mutate)
    {
        if (jobs.TryGetValue(jobId, out var job))
        {
            mutate(job);
        }
    }
}
