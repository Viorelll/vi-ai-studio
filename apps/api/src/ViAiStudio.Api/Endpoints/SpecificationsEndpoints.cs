using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Common;
using ViAiStudio.Application.Specifications;
using ViAiStudio.Domain.Catalog;

namespace ViAiStudio.Api.Endpoints;

public static class SpecificationsEndpoints
{
    public static void MapSpecificationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/specification-phase-catalog", () =>
            Results.Ok(SpecificationPhaseCatalog.Phases.Select(SpecificationPhaseDefinitionResponse.FromDefinition)))
            .RequireAuthorization();

        var group = app.MapGroup("/api/specifications").RequireAuthorization();

        group.MapGet("/", async (ISpecificationRepository repository, CancellationToken cancellationToken) =>
        {
            var specifications = await repository.ListAsync(cancellationToken);
            return Results.Ok(specifications.Select(SpecificationSummaryResponse.FromEntity));
        });

        group.MapPost("/", async (CreateSpecificationRequest request, CreateSpecificationHandler handler, CancellationToken cancellationToken) =>
        {
            var command = new CreateSpecificationCommand(request.Name, request.Summary, request.Owner ?? "You");
            var specification = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/api/specifications/{specification.Id}", SpecificationDetailResponse.FromEntity(specification));
        });

        group.MapGet("/{id:guid}", async (Guid id, ISpecificationRepository repository, CancellationToken cancellationToken) =>
        {
            var specification = await repository.GetAsync(id, cancellationToken);
            return specification is not null ? Results.Ok(SpecificationDetailResponse.FromEntity(specification)) : Results.NotFound();
        });

        group.MapPatch("/{id:guid}", async (Guid id, UpdateSpecificationBasicsRequest request, UpdateSpecificationBasicsHandler handler, CancellationToken cancellationToken) =>
        {
            var command = new UpdateSpecificationBasicsCommand(id, request.Name, request.Summary, request.Audience, request.Stack?.ToValue());
            var specification = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(SpecificationDetailResponse.FromEntity(specification));
        });

        group.MapDelete("/{id:guid}", async (Guid id, DeleteSpecificationHandler handler, CancellationToken cancellationToken) =>
        {
            var deleted = await handler.HandleAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapPut("/{id:guid}/phases/{phaseIndex:int}", async (
            Guid id, int phaseIndex, SaveSpecificationPhaseRequest request, SaveSpecificationPhaseHandler handler, CancellationToken cancellationToken) =>
        {
            var command = new SaveSpecificationPhaseCommand(id, phaseIndex, request.CheckedItems, request.SelectedKeywords);
            var phase = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(SpecificationPhaseResponse.FromEntity(phase));
        });

        group.MapPost("/{id:guid}/phases/{phaseIndex:int}/generate", async (
            Guid id, int phaseIndex, GeneratePhaseTextHandler handler, CancellationToken cancellationToken) =>
        {
            var phase = await handler.HandleAsync(new GeneratePhaseTextCommand(id, phaseIndex), cancellationToken);
            return Results.Ok(SpecificationPhaseResponse.FromEntity(phase));
        });

        group.MapPost("/{id:guid}/finalize", async (Guid id, FinalizeSpecificationHandler handler, CancellationToken cancellationToken) =>
        {
            var specification = await handler.HandleAsync(new FinalizeSpecificationCommand(id), cancellationToken);
            return Results.Ok(SpecificationDetailResponse.FromEntity(specification));
        });

        group.MapGet("/{id:guid}/download", async (Guid id, ISpecificationRepository repository, CancellationToken cancellationToken) =>
        {
            var specification = await repository.GetAsync(id, cancellationToken);
            if (specification is null)
            {
                return Results.NotFound();
            }
            if (string.IsNullOrWhiteSpace(specification.SpecMarkdown))
            {
                return Results.BadRequest(new { error = "Finalize the specification before downloading it." });
            }

            using var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry("specification.md", CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(specification.SpecMarkdown);
            }

            var fileName = Slugify(specification.Name) + "-specification.zip";
            return Results.File(stream.ToArray(), "application/zip", fileName);
        });
    }

    internal static string Slugify(string name) =>
        Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
}
