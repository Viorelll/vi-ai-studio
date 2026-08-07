using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Api.Contracts;

public sealed record AiCallLogResponse(
    Guid Id,
    Guid SpecificationId,
    int? GenerationVersion,
    AiTaskType Task,
    string Model,
    int Requests,
    int TokensIn,
    int TokensOut,
    DateTimeOffset Created)
{
    public static AiCallLogResponse FromEntity(AiCallLog log) => new(
        log.Id, log.SpecificationId, log.GenerationVersion, log.Task, log.Model,
        log.Requests, log.TokensIn, log.TokensOut, log.CreatedAt);
}

public sealed record AiCallLogDetailResponse(
    Guid Id,
    AiTaskType Task,
    string Model,
    int Requests,
    int TokensIn,
    int TokensOut,
    string Prompt,
    string Result,
    DateTimeOffset Created)
{
    public static AiCallLogDetailResponse FromEntity(AiCallLog log) => new(
        log.Id, log.Task, log.Model, log.Requests, log.TokensIn, log.TokensOut, log.Prompt, log.Result, log.CreatedAt);
}

public sealed record AiCallLogRollupResponse(Guid SpecificationId, int LogCount, int TotalRequests, long TotalTokens)
{
    public static AiCallLogRollupResponse FromRollup(AiCallLogRollup rollup) =>
        new(rollup.SpecificationId, rollup.LogCount, rollup.TotalRequests, rollup.TotalTokens);
}
