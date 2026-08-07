using System.Text.Json;
using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Builds;
using ViAiStudio.Application.Common;

namespace ViAiStudio.Api.Endpoints;

public static class BuildsEndpoints
{
    private static readonly JsonSerializerOptions SseJsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapBuildsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/specifications/{id:guid}/builds", async (
            Guid id, StartBuildRequest request, StartBuildHandler handler, CancellationToken cancellationToken) =>
        {
            var generation = await handler.HandleAsync(new StartBuildCommand(id, request.AiModelConfigId), cancellationToken);
            return Results.Created($"/api/generations/{generation.Id}", GenerationSummaryResponse.FromEntity(generation));
        });

        app.MapGet("/api/specifications/{id:guid}/builds", async (
            Guid id, IGenerationRepository repository, CancellationToken cancellationToken) =>
        {
            var generations = await repository.ListBySpecificationAsync(id, cancellationToken);
            return Results.Ok(generations.Select(GenerationSummaryResponse.FromEntity));
        });

        app.MapGet("/api/generated-projects", async (ISpecificationRepository repository, CancellationToken cancellationToken) =>
        {
            var specifications = await repository.ListAsync(cancellationToken);
            return Results.Ok(specifications
                .Where(s => s.Generations.Count > 0)
                .Select(SpecificationSummaryResponse.FromEntity));
        });

        app.MapGet("/api/generations/{id:guid}", async (Guid id, IGenerationRepository repository, CancellationToken cancellationToken) =>
        {
            var generation = await repository.GetAsync(id, cancellationToken);
            return generation is not null ? Results.Ok(GenerationDetailResponse.FromEntity(generation)) : Results.NotFound();
        });

        app.MapGet("/api/generations/{id:guid}/download", async (
            Guid id, IGenerationRepository repository, IBlobStorage blobStorage, CancellationToken cancellationToken) =>
        {
            var generation = await repository.GetAsync(id, cancellationToken);
            if (generation is null)
            {
                return Results.NotFound();
            }
            if (string.IsNullOrWhiteSpace(generation.ArchiveStorageKey))
            {
                return Results.BadRequest(new { error = "This build has no archive yet." });
            }

            var url = await blobStorage.CreatePresignedDownloadAsync(generation.ArchiveStorageKey, cancellationToken);
            return Results.Redirect(url);
        });

        // Live build log for the AI Build page: one server-sent event per pipeline
        // step, terminated once CompleteBuildHandler publishes a Done event.
        app.MapGet("/api/generations/{id:guid}/stream", async (Guid id, HttpContext context, IBuildEventBroadcaster broadcaster) =>
        {
            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";

            await foreach (var buildEvent in broadcaster.SubscribeAsync(id, context.RequestAborted))
            {
                var payload = JsonSerializer.Serialize(buildEvent, SseJsonOptions);
                await context.Response.WriteAsync($"data: {payload}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        });
    }
}
