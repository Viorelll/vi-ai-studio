using ViAiStudio.Domain.Entities;
using ViAiStudio.Domain.ValueObjects;

namespace ViAiStudio.Api.Contracts;

public sealed record TechStackRequest(string Backend, string Ui, string Database, string Infra, string UiStyle)
{
    public TechStack ToValue() => new(Backend, Ui, Database, Infra, UiStyle);
}

public sealed record CreateSpecificationRequest(string Name, string Summary, string? Owner);

public sealed record UpdateSpecificationBasicsRequest(string? Name, string? Summary, string? Audience, TechStackRequest? Stack);

public sealed record CreateAiModelConfigRequest(string Label, AiProvider Provider, string Model, string BaseUrl, string ApiKey);

public sealed record UpdateAiModelConfigRequest(string Label, AiProvider Provider, string Model, string BaseUrl, string ApiKey);

public sealed record UpdateTaskRoutingRequest(Guid? AiModelConfigId);

public sealed record StartBuildRequest(Guid? AiModelConfigId);

/// <summary>Body AI Generator posts to the internal build-events webhook after each pipeline step.</summary>
public sealed record RecordBuildProgressRequest(string StepLabel, string LogLine, int ProgressPct);

public sealed record BuildAiCallReportRequest(string Model, int TokensIn, int TokensOut, string Prompt, string Result);

/// <summary>Body AI Generator posts to the internal build-events webhook once a build finishes or fails.</summary>
public sealed record CompleteBuildRequest(
    bool Success,
    string? FailureReason,
    int DurationSeconds,
    List<string> FileTree,
    string? ArchiveStorageKey,
    List<BuildAiCallReportRequest> AiCalls);
