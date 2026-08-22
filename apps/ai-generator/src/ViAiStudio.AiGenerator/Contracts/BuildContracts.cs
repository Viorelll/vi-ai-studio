namespace ViAiStudio.AiGenerator.Contracts;

public sealed record StackDto(string Backend, string Ui, string Database, string Infra, string UiStyle);

/// <summary>
/// One authored specification file. The front-matter fields travel alongside
/// the markdown rather than being re-parsed out of it here: the Api already
/// holds them as structured columns, and the build pipeline plans its phases,
/// orders them by dependency and measures spec coverage from these fields. A
/// generator that only received <see cref="Content"/> would have to re-parse
/// YAML just to rediscover what the Api already knows.
/// </summary>
public sealed record SpecDocumentDto(
    string Path,
    string Content,
    string SpecId = "",
    string Title = "",
    string Component = "",
    IReadOnlyList<string>? DependsOn = null,
    IReadOnlyList<string>? Provides = null,
    IReadOnlyList<string>? Generates = null);

/// <summary>
/// The complete build brief. AI Generator is stateless -- it never reads the
/// Api's database -- so everything the model needs to generate the project
/// (the authored specification in full, plus the tech stack to build it on)
/// arrives here, along with the per-call model credentials.
/// </summary>
public sealed record StartBuildRequest(
    Guid GenerationId,
    string Provider,
    string Model,
    string BaseUrl,
    string ApiKey,
    string SpecificationName,
    string Summary,
    string Description,
    string Audience,
    string Features,
    string SpecMarkdown,
    StackDto Stack,
    IReadOnlyList<SpecDocumentDto> Documents,
    string CallbackBaseUrl);

public sealed record StartBuildResponse(string JobId);

public sealed record BuildJobStatusResponse(string JobId, Guid GenerationId, string Status, int ProgressPct, string CurrentStep);
