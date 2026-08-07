using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Builds;

namespace ViAiStudio.Api.Endpoints;

/// <summary>
/// Internal webhook AI Generator calls back on while (and after) running a
/// build. Guarded by a shared secret rather than user auth -- only AI
/// Generator should ever call these.
/// </summary>
public static class BuildEventsEndpoints
{
    public static void MapBuildEventsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internal/builds").AddEndpointFilter(async (context, next) =>
        {
            var expected = context.HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["AiGenerator:WebhookSecret"];
            var provided = context.HttpContext.Request.Headers["X-Internal-Secret"].ToString();

            if (string.IsNullOrEmpty(expected) || provided != expected)
            {
                return Results.Unauthorized();
            }

            return await next(context);
        });

        group.MapPost("/{generationId:guid}/events", async (
            Guid generationId, RecordBuildProgressRequest request, RecordBuildProgressHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new RecordBuildProgressCommand(generationId, request.StepLabel, request.LogLine, request.ProgressPct), cancellationToken);
            return Results.Accepted();
        });

        group.MapPost("/{generationId:guid}/complete", async (
            Guid generationId, CompleteBuildRequest request, CompleteBuildHandler handler, CancellationToken cancellationToken) =>
        {
            var command = new CompleteBuildCommand(
                generationId,
                request.Success,
                request.FailureReason,
                request.DurationSeconds,
                request.FileTree,
                request.ArchiveStorageKey,
                request.AiCalls.Select(c => new BuildAiCallReport(c.Model, c.TokensIn, c.TokensOut, c.Prompt, c.Result)).ToList());

            var generation = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(GenerationDetailResponse.FromEntity(generation));
        });
    }
}
