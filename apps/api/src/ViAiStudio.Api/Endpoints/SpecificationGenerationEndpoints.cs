using System.Text.Json;
using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Common;
using ViAiStudio.Application.Specifications;

namespace ViAiStudio.Api.Endpoints;

/// <summary>Stage 3 (batch generation) of the specification wizard.</summary>
public static class SpecificationGenerationEndpoints
{
    private static readonly JsonSerializerOptions SseJsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapSpecificationGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/specifications/{id:guid}/generation-runs").RequireAuthorization();

        group.MapPost("/", async (Guid id, StartSpecificationGenerationHandler handler, CancellationToken cancellationToken) =>
        {
            var run = await handler.HandleAsync(new StartSpecificationGenerationCommand(id), cancellationToken);
            return Results.Created(
                $"/api/specifications/{id}/generation-runs/{run.Id}", SpecificationGenerationRunResponse.FromEntity(run));
        });

        group.MapGet("/", async (Guid id, ISpecificationGenerationRunRepository runRepository, CancellationToken cancellationToken) =>
        {
            var runs = await runRepository.ListAsync(id, cancellationToken);
            return Results.Ok(runs.Select(SpecificationGenerationRunResponse.FromEntity));
        });

        group.MapGet("/{runId:guid}", async (
            Guid runId, ISpecificationGenerationRunRepository runRepository, CancellationToken cancellationToken) =>
        {
            var run = await runRepository.GetAsync(runId, cancellationToken);
            return run is not null ? Results.Ok(SpecificationGenerationRunResponse.FromEntity(run)) : Results.NotFound();
        });

        // Live batch progress for the stage-3 UI: one server-sent event per
        // batch state change, terminated once RunSpecificationGenerationHandler
        // publishes a Done event. Byte-for-byte the same writer loop as AI
        // Build's /api/generations/{id}/stream.
        group.MapGet("/{runId:guid}/stream", async (Guid runId, HttpContext context, IBuildEventBroadcaster broadcaster) =>
        {
            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";

            await foreach (var buildEvent in broadcaster.SubscribeAsync(runId, context.RequestAborted))
            {
                var payload = JsonSerializer.Serialize(buildEvent, SseJsonOptions);
                await context.Response.WriteAsync($"data: {payload}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        });

        app.MapGet("/api/specifications/{id:guid}/validation-issues", async (
            Guid id, ISpecificationValidationIssueRepository issueRepository, CancellationToken cancellationToken) =>
        {
            var issues = await issueRepository.ListAsync(id, cancellationToken);
            return Results.Ok(issues.Select(ValidationIssueResponse.FromEntity));
        }).RequireAuthorization();
    }
}
