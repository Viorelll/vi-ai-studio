using ViAiStudio.AiGenerator.Builds;
using ViAiStudio.AiGenerator.Contracts;

namespace ViAiStudio.AiGenerator.Endpoints;

public static class BuildsEndpoints
{
    public static void MapBuildsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/builds", (StartBuildRequest request, IBuildJobStore jobStore, BuildQueue queue) =>
        {
            var job = jobStore.Create(request.GenerationId);
            queue.Enqueue(new QueuedBuild(job.JobId, request));
            return Results.Accepted(value: new StartBuildResponse(job.JobId));
        });

        app.MapGet("/v1/builds/{jobId}", (string jobId, IBuildJobStore jobStore) =>
        {
            var job = jobStore.Get(jobId);
            return job is not null
                ? Results.Ok(new BuildJobStatusResponse(job.JobId, job.GenerationId, job.Status.ToString(), job.ProgressPct, job.CurrentStep))
                : Results.NotFound();
        });
    }
}
