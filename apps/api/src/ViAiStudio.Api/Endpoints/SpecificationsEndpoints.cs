using System.Text.RegularExpressions;
using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Common;
using ViAiStudio.Application.Specifications;

namespace ViAiStudio.Api.Endpoints;

public static class SpecificationsEndpoints
{
    public static void MapSpecificationsEndpoints(this IEndpointRouteBuilder app)
    {
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

        // Backs the "Specifications (.md)" file tree + preview pane on the spec
        // detail page and the wizard's live file list. Documents are persisted
        // rows (written by the stage-3 batch loop, see
        // RunSpecificationGenerationHandler), so these are direct, cheap reads --
        // no MinIO round-trip and no on-the-fly rendering needed here.
        group.MapGet("/{id:guid}/documents", async (
            Guid id, ISpecificationRepository repository, ISpecificationDocumentRepository documentRepository, CancellationToken cancellationToken) =>
        {
            if (!await repository.ExistsAsync(id, cancellationToken))
            {
                return Results.NotFound();
            }
            var documents = await documentRepository.ListAsync(id, cancellationToken);
            return Results.Ok(documents.Select(d => d.Path));
        });

        group.MapGet("/{id:guid}/documents/content", async (
            Guid id, string path, ISpecificationDocumentRepository documentRepository, CancellationToken cancellationToken) =>
        {
            var document = await documentRepository.GetAsync(id, path, cancellationToken);
            if (document is null)
            {
                return Results.NotFound();
            }
            return Results.Ok(new { path = document.Path, content = document.Content });
        });

        // Streams the archive the batch loop already zipped and uploaded to
        // MinIO after the last completed batch -- never rebuilt on GET.
        group.MapGet("/{id:guid}/download", async (
            Guid id, ISpecificationRepository repository, IBlobStorage blobStorage, CancellationToken cancellationToken) =>
        {
            var specification = await repository.GetAsync(id, cancellationToken);
            if (specification is null)
            {
                return Results.NotFound();
            }
            if (string.IsNullOrWhiteSpace(specification.DocumentsArchiveStorageKey))
            {
                return Results.BadRequest(new { error = "Generate the specification before downloading it." });
            }

            var archive = await blobStorage.DownloadAsync(specification.DocumentsArchiveStorageKey, cancellationToken);
            var fileName = Slugify(specification.Name) + "-specification.zip";
            return Results.File(archive, "application/zip", fileName);
        });
    }

    internal static string Slugify(string name) =>
        Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
}
