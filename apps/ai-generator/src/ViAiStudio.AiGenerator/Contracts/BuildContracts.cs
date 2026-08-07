namespace ViAiStudio.AiGenerator.Contracts;

public sealed record StackDto(string Backend, string Ui, string Database, string Infra, string UiStyle);

public sealed record StartBuildRequest(
    Guid GenerationId,
    string Provider,
    string Model,
    string BaseUrl,
    string ApiKey,
    string SpecificationName,
    string SpecMarkdown,
    StackDto Stack,
    string CallbackBaseUrl);

public sealed record StartBuildResponse(string JobId);

public sealed record BuildJobStatusResponse(string JobId, Guid GenerationId, string Status, int ProgressPct, string CurrentStep);
