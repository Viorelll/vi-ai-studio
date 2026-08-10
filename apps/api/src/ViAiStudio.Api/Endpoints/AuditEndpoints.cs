using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Common;

namespace ViAiStudio.Api.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/audit")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/specifications", async (string? scope, IAiCallLogRepository repository, CancellationToken cancellationToken) =>
        {
            var rollups = await repository.RollupBySpecificationAsync(ParseScope(scope), cancellationToken);
            return Results.Ok(rollups.Select(AiCallLogRollupResponse.FromRollup));
        });

        group.MapGet("/specifications/{id:guid}", async (Guid id, string? scope, IAiCallLogRepository repository, CancellationToken cancellationToken) =>
        {
            var logs = await repository.ListBySpecificationAsync(id, ParseScope(scope), cancellationToken);
            return Results.Ok(logs.Select(AiCallLogResponse.FromEntity));
        });

        group.MapGet("/logs/{id:guid}", async (Guid id, IAiCallLogRepository repository, CancellationToken cancellationToken) =>
        {
            var log = await repository.GetAsync(id, cancellationToken);
            return log is not null ? Results.Ok(AiCallLogDetailResponse.FromEntity(log)) : Results.NotFound();
        });
    }

    /// <summary>
    /// Unrecognised or missing values fall back to <see cref="AiCallLogScope.All"/>
    /// rather than erroring, so an old client keeps seeing the combined totals
    /// it always did instead of an empty table.
    /// </summary>
    private static AiCallLogScope ParseScope(string? scope) =>
        Enum.TryParse<AiCallLogScope>(scope, ignoreCase: true, out var parsed) ? parsed : AiCallLogScope.All;
}
