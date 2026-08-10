using Microsoft.Extensions.Options;
using ViAiStudio.AiGenerator.Generation;

namespace ViAiStudio.AiGenerator.Sandbox;

public sealed record VerificationStep(string Name, string StartMessage);

/// <summary>
/// The gate a generated project has to pass before a build is called done:
/// the backend compiles, the frontend compiles, and the two-tier stack
/// actually boots against a real database and answers its health endpoint.
/// Each step returns the raw container output on failure, which is exactly
/// what gets fed back to the model to repair.
/// </summary>
public sealed class ProjectVerifier(ISandboxExecutor sandbox, IOptions<SandboxOptions> options)
{
    private readonly SandboxOptions options = options.Value;

    private const string DatabaseName = "appdb";
    private const string DatabaseUser = "appuser";
    private const string DatabasePassword = "apppass";
    private const string DatabaseAlias = "db";

    public static readonly VerificationStep BackendBuild = new("Backend build", "Compiling the backend…");
    public static readonly VerificationStep FrontendBuild = new("Frontend build", "Compiling the frontend…");
    public static readonly VerificationStep IntegrationRun = new("Integration run", "Booting the stack against a database…");

    public IReadOnlyList<VerificationStep> Steps => [BackendBuild, FrontendBuild, IntegrationRun];

    public Task<SandboxRunResult> RunAsync(VerificationStep step, string workspaceHostPath, CancellationToken cancellationToken)
    {
        if (step == BackendBuild) return RunBackendBuildAsync(workspaceHostPath, cancellationToken);
        if (step == FrontendBuild) return RunFrontendBuildAsync(workspaceHostPath, cancellationToken);
        if (step == IntegrationRun) return RunIntegrationAsync(workspaceHostPath, cancellationToken);
        throw new ArgumentOutOfRangeException(nameof(step), step.Name, "Unknown verification step.");
    }

    private Task<SandboxRunResult> RunBackendBuildAsync(string workspaceHostPath, CancellationToken cancellationToken) =>
        sandbox.RunAsync(new SandboxRun(
            options.BackendImage,
            "dotnet restore && dotnet build -c Release",
            workspaceHostPath,
            ProjectLayout.BackendDirectory,
            // Keeps each container's restore self-contained and quiet; the
            // welcome banner and telemetry notice otherwise pollute the error
            // log the model has to read.
            new Dictionary<string, string>
            {
                ["DOTNET_NOLOGO"] = "1",
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
            }),
            cancellationToken);

    private Task<SandboxRunResult> RunFrontendBuildAsync(string workspaceHostPath, CancellationToken cancellationToken) =>
        sandbox.RunAsync(new SandboxRun(
            options.FrontendImage,
            "npm install --no-audit --no-fund && npm run build",
            workspaceHostPath,
            ProjectLayout.FrontendDirectory,
            new Dictionary<string, string>
            {
                ["CI"] = "1",
                ["NEXT_TELEMETRY_DISABLED"] = "1",
            }),
            cancellationToken);

    /// <summary>
    /// The real stop condition: a Postgres container comes up on a private
    /// network, the generated backend is started against it, and its health
    /// endpoint has to answer 200 within the probe window.
    /// </summary>
    private async Task<SandboxRunResult> RunIntegrationAsync(string workspaceHostPath, CancellationToken cancellationToken)
    {
        await using var network = await sandbox.CreateNetworkAsync(cancellationToken);

        await network.StartServiceAsync(
            options.DatabaseImage,
            DatabaseAlias,
            new Dictionary<string, string>
            {
                ["POSTGRES_DB"] = DatabaseName,
                ["POSTGRES_USER"] = DatabaseUser,
                ["POSTGRES_PASSWORD"] = DatabasePassword,
            },
            cancellationToken);

        return await sandbox.RunAsync(new SandboxRun(
            options.BackendImage,
            HealthProbeScript,
            workspaceHostPath,
            ProjectLayout.BackendDirectory,
            new Dictionary<string, string>
            {
                ["DOTNET_NOLOGO"] = "1",
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                [ProjectLayout.ConnectionStringEnvVar] =
                    $"Host={DatabaseAlias};Port=5432;Database={DatabaseName};Username={DatabaseUser};Password={DatabasePassword}",
                ["ASPNETCORE_URLS"] = $"http://0.0.0.0:{ProjectLayout.BackendPort}",
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            },
            network.Name),
            cancellationToken);
    }

    /// <summary>
    /// Starts the app in the background and polls its health endpoint. The
    /// application log is always echoed -- on failure it is the only thing that
    /// explains *why* the boot failed, and it's what the repair prompt gets.
    /// </summary>
    private static readonly string HealthProbeScript = $"""
        set -e
        dotnet restore
        dotnet build -c Release
        echo 'Waiting for the database to accept connections…'
        sleep 12
        dotnet run --no-build -c Release > /tmp/app.log 2>&1 &
        healthy=0
        for attempt in $(seq 1 45); do
          sleep 2
          if curl -fsS http://127.0.0.1:{ProjectLayout.BackendPort}{ProjectLayout.HealthPath} > /dev/null 2>&1; then healthy=1; break; fi
          if wget -q -O - http://127.0.0.1:{ProjectLayout.BackendPort}{ProjectLayout.HealthPath} > /dev/null 2>&1; then healthy=1; break; fi
        done
        echo '----- application log -----'
        cat /tmp/app.log || true
        if [ "$healthy" = "1" ]; then
          echo 'HEALTH_CHECK_PASSED'
          exit 0
        fi
        echo 'HEALTH_CHECK_FAILED: {ProjectLayout.HealthPath} did not return 200 within 90 seconds.'
        exit 1
        """;
}
