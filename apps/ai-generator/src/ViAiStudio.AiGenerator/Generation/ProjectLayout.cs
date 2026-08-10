namespace ViAiStudio.AiGenerator.Generation;

/// <summary>
/// The contract every generated project must satisfy. It is embedded verbatim
/// into the generation prompt *and* is what <c>ProjectVerifier</c> actually
/// runs, so the two can never drift: if the model honours this, the
/// verification steps are guaranteed to know where to look. Without a fixed
/// layout there is no deterministic way to decide whether a freshly invented
/// project "builds".
/// </summary>
public static class ProjectLayout
{
    public const string BackendDirectory = "backend";
    public const string FrontendDirectory = "frontend";
    public const int BackendPort = 8080;
    public const string HealthPath = "/health";
    public const string ConnectionStringEnvVar = "ConnectionStrings__Postgres";

    /// <summary>Prompt fragment describing the non-negotiable structure of the output.</summary>
    public static readonly string Contract = $"""
        The generated repository MUST follow this exact structure, because an automated
        pipeline builds and runs it without human intervention:

        - `{BackendDirectory}/` - the backend service. The runnable API host project file
          MUST sit directly at `{BackendDirectory}/` (so `dotnet build` and `dotnet run` work
          when executed from that directory, with no --project argument); any additional
          class libraries live in subdirectories and are referenced from it. It must listen
          on port {BackendPort}, expose a `GET {HealthPath}` endpoint returning HTTP 200, and read
          its database connection string from the `{ConnectionStringEnvVar}` environment
          variable (standard ASP.NET Core configuration binding).
        - `{FrontendDirectory}/` - the frontend app. It must have a package.json whose
          `build` script succeeds under `npm install && npm run build` with no network
          access beyond the npm registry, and no interactive prompts.
        - `docker-compose.yml` at the repository root wiring backend, frontend and database.
        - `README.md` at the repository root.

        Hard requirements:
        - The backend must start successfully even on a completely empty database
          (apply migrations or create the schema on startup).
        - Do not use any paid, licensed or credential-requiring third-party service.
        - Pin package versions that actually exist and are mutually compatible.
        - Every file must be complete and compilable. Never emit placeholders,
          "..." elisions, or TODO stubs in place of real code.
        """;
}
