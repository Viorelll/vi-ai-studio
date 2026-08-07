using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Common;

namespace ViAiStudio.Api.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/audit");

        group.MapGet("/specifications", async (IAiCallLogRepository repository, CancellationToken cancellationToken) =>
        {
            var rollups = await repository.RollupBySpecificationAsync(cancellationToken);
            return Results.Ok(rollups.Select(AiCallLogRollupResponse.FromRollup));
        });

        group.MapGet("/specifications/{id:guid}", async (Guid id, IAiCallLogRepository repository, CancellationToken cancellationToken) =>
        {
            var logs = await repository.ListBySpecificationAsync(id, cancellationToken);
            return Results.Ok(logs.Select(AiCallLogResponse.FromEntity));
        });

        group.MapGet("/logs/{id:guid}", async (Guid id, IAiCallLogRepository repository, CancellationToken cancellationToken) =>
        {
            var log = await repository.GetAsync(id, cancellationToken);
            return log is not null ? Results.Ok(AiCallLogDetailResponse.FromEntity(log)) : Results.NotFound();
        });
    }
}
