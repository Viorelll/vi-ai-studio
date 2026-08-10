using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Admin;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var configs = app.MapGroup("/api/admin/ai-configs")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        configs.MapGet("/", async (IAiModelConfigRepository repository, CancellationToken cancellationToken) =>
        {
            var list = await repository.ListAsync(cancellationToken);
            return Results.Ok(list.Select(AiModelConfigResponse.FromEntity));
        });

        configs.MapPost("/", async (CreateAiModelConfigRequest request, CreateAiModelConfigHandler handler, CancellationToken cancellationToken) =>
        {
            var command = new CreateAiModelConfigCommand(request.Label, request.Provider, request.Model, request.BaseUrl, request.ApiKey);
            var config = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/api/admin/ai-configs/{config.Id}", AiModelConfigResponse.FromEntity(config));
        });

        configs.MapPut("/{id:guid}", async (Guid id, UpdateAiModelConfigRequest request, UpdateAiModelConfigHandler handler, CancellationToken cancellationToken) =>
        {
            var command = new UpdateAiModelConfigCommand(id, request.Label, request.Provider, request.Model, request.BaseUrl, request.ApiKey);
            var config = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(AiModelConfigResponse.FromEntity(config));
        });

        configs.MapDelete("/{id:guid}", async (Guid id, DeleteAiModelConfigHandler handler, CancellationToken cancellationToken) =>
        {
            var deleted = await handler.HandleAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        configs.MapGet("/{id:guid}/reveal", async (Guid id, IAiModelConfigRepository repository, CancellationToken cancellationToken) =>
        {
            var config = await repository.GetAsync(id, cancellationToken);
            return config is not null ? Results.Ok(new { apiKey = config.ApiKey }) : Results.NotFound();
        });

        var routing = app.MapGroup("/api/admin/task-routing")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        routing.MapGet("/", async (ITaskRoutingRepository repository, CancellationToken cancellationToken) =>
        {
            var list = await repository.ListAsync(cancellationToken);
            return Results.Ok(list.Select(TaskRoutingResponse.FromEntity));
        });

        routing.MapPut("/{task}", async (string task, UpdateTaskRoutingRequest request, UpdateTaskRoutingHandler handler, CancellationToken cancellationToken) =>
        {
            // Minimal APIs bind route parameters with a case-sensitive Enum.Parse that
            // knows nothing about ConfigureHttpJsonOptions's camelCase converter -- "codeGeneration"
            // in the URL would otherwise fail to bind to AiTaskType.CodeGeneration. Parsing it here
            // with ignoreCase keeps the route in the same camelCase casing as the JSON wire format.
            if (!Enum.TryParse<AiTaskType>(task, ignoreCase: true, out var taskType))
            {
                return Results.NotFound();
            }

            var updated = await handler.HandleAsync(new UpdateTaskRoutingCommand(taskType, request.AiModelConfigId), cancellationToken);
            return Results.Ok(TaskRoutingResponse.FromEntity(updated));
        });

    }
}
