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
    public static readonly VerificationStep AutomatedTests = new("Automated tests", "Running the generated test suite…");
    public static readonly VerificationStep IntegrationRun = new("Integration run", "Booting the stack against a database…");

    /// <summary>
    /// The always-run steps. <see cref="AutomatedTests"/> is deliberately not
    /// here: it only makes sense when the specification actually asked for
    /// tests, so the orchestrator adds it based on the build plan rather than
    /// failing every project whose specification never mentioned testing.
    /// </summary>
    public IReadOnlyList<VerificationStep> Steps => [BackendBuild, FrontendBuild, IntegrationRun];

    public Task<SandboxRunResult> RunAsync(VerificationStep step, string workspaceHostPath, CancellationToken cancellationToken)
    {
        if (step == BackendBuild) return RunBackendBuildAsync(workspaceHostPath, cancellationToken);
        if (step == FrontendBuild) return RunFrontendBuildAsync(workspaceHostPath, cancellationToken);
        if (step == AutomatedTests) return RunTestsAsync(workspaceHostPath, cancellationToken);
        if (step == IntegrationRun) return RunIntegrationAsync(workspaceHostPath, cancellationToken);
        throw new ArgumentOutOfRangeException(nameof(step), step.Name, "Unknown verification step.");
    }

    /// <summary>
    /// Where the NuGet global-packages folder and npm's cache live inside every
    /// sandbox container. Bound to named Docker volumes (see <see cref="SandboxOptions"/>)
    /// so restore doesn't hit the network from a cold container on every build
    /// and every repair round.
    /// </summary>
    private const string NugetCacheMountPath = "/root/.nuget/packages";
    private const string NpmCacheMountPath = "/root/.npm";

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
                ["NUGET_PACKAGES"] = NugetCacheMountPath,
            },
            AdditionalBinds: [$"{options.NugetCacheVolume}:{NugetCacheMountPath}"]),
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
            },
            // node:22-alpine's default user is root, whose home (and npm's
            // default cache dir) is /root -- mounting the cache volume there
            // needs no extra npm config.
            AdditionalBinds: [$"{options.NpmCacheVolume}:{NpmCacheMountPath}"]),
            cancellationToken);

    /// <summary>
    /// Runs the tests the generation phase wrote for the specification's
    /// acceptance criteria. A database sidecar is on the network and its
    /// connection string is in the environment, so tests that exercise real
    /// persistence work rather than forcing every generated test to be a pure
    /// unit test.
    /// </summary>
    private async Task<SandboxRunResult> RunTestsAsync(string workspaceHostPath, CancellationToken cancellationToken)
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
            TestScript,
            workspaceHostPath,
            ProjectLayout.BackendDirectory,
            new Dictionary<string, string>
            {
                ["DOTNET_NOLOGO"] = "1",
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                ["NUGET_PACKAGES"] = NugetCacheMountPath,
                [ProjectLayout.ConnectionStringEnvVar] =
                    $"Host={DatabaseAlias};Port=5432;Database={DatabaseName};Username={DatabaseUser};Password={DatabasePassword}",
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            },
            network.Name,
            AdditionalBinds: [$"{options.NugetCacheVolume}:{NugetCacheMountPath}"]),
            cancellationToken);
    }

    /// <summary>
    /// The real stop condition: a Postgres container comes up on a private
    /// network, the generated backend is started against it, its health
    /// endpoint has to answer within the probe window, its OpenAPI document
    /// has to list a real endpoint, a smoke call against that endpoint must
    /// not 500, and the database must actually have tables in it -- "booted"
    /// is not the same claim as "the migrations ran and something works".
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

        var appResult = await sandbox.RunAsync(new SandboxRun(
            options.BackendImage,
            HealthProbeScript,
            workspaceHostPath,
            ProjectLayout.BackendDirectory,
            new Dictionary<string, string>
            {
                ["DOTNET_NOLOGO"] = "1",
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                ["NUGET_PACKAGES"] = NugetCacheMountPath,
                [ProjectLayout.ConnectionStringEnvVar] =
                    $"Host={DatabaseAlias};Port=5432;Database={DatabaseName};Username={DatabaseUser};Password={DatabasePassword}",
                ["ASPNETCORE_URLS"] = $"http://0.0.0.0:{ProjectLayout.BackendPort}",
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            },
            network.Name,
            AdditionalBinds: [$"{options.NugetCacheVolume}:{NugetCacheMountPath}"]),
            cancellationToken);

        if (!appResult.Succeeded) return appResult;

        // The app can only pass its own health probe by starting -- it says
        // nothing about whether it created a schema. Ask Postgres directly,
        // while the network (and its data) are still up, rather than trusting
        // the app's word for it.
        var schemaResult = await sandbox.RunAsync(new SandboxRun(
            options.DatabaseImage,
            SchemaCheckScript,
            workspaceHostPath,
            WorkingSubdirectory: null,
            new Dictionary<string, string>
            {
                ["PGPASSWORD"] = DatabasePassword,
            },
            network.Name),
            cancellationToken);

        var combinedOutput = $"{appResult.Output}\n\n----- migrations check -----\n{schemaResult.Output}";
        return schemaResult.Succeeded
            ? appResult with { Output = combinedOutput }
            : schemaResult with { Output = combinedOutput };
    }

    /// <summary>
    /// Starts the app in the background and polls its health endpoint, then --
    /// only once healthy -- checks that its OpenAPI document lists a real
    /// endpoint and smoke-tests that endpoint. The application log is always
    /// echoed; on failure it is the only thing that explains *why* the boot
    /// failed, and it's what the repair prompt gets.
    /// </summary>
    private static readonly string HealthProbeScript = $$"""
        set -e
        dotnet restore
        dotnet build -c Release
        echo 'Waiting for the database to accept connections…'
        sleep 12
        dotnet run --no-build -c Release > /tmp/app.log 2>&1 &
        healthy=0
        for attempt in $(seq 1 45); do
          sleep 2
          if curl -fsS http://127.0.0.1:{{ProjectLayout.BackendPort}}{{ProjectLayout.HealthPath}} > /dev/null 2>&1; then healthy=1; break; fi
          if wget -q -O - http://127.0.0.1:{{ProjectLayout.BackendPort}}{{ProjectLayout.HealthPath}} > /dev/null 2>&1; then healthy=1; break; fi
        done
        echo '----- application log -----'
        cat /tmp/app.log || true
        if [ "$healthy" != "1" ]; then
          echo 'HEALTH_CHECK_FAILED: {{ProjectLayout.HealthPath}} did not return 200 within 90 seconds.'
          exit 1
        fi
        echo 'HEALTH_CHECK_PASSED'

        echo '----- checking the OpenAPI document -----'
        if ! (curl -fsS http://127.0.0.1:{{ProjectLayout.BackendPort}}{{ProjectLayout.OpenApiPath}} -o /tmp/openapi.json 2>&1 \
              || wget -q -O /tmp/openapi.json http://127.0.0.1:{{ProjectLayout.BackendPort}}{{ProjectLayout.OpenApiPath}} 2>&1); then
          echo 'OPENAPI_CHECK_FAILED: could not fetch {{ProjectLayout.OpenApiPath}}.'
          exit 1
        fi
        other_paths=$(grep -oE '"(/[^"]*)"[[:space:]]*:[[:space:]]*\{' /tmp/openapi.json \
          | sed -E 's/^"(.*)"[[:space:]]*:.*$/\1/' \
          | grep -v '^{{ProjectLayout.HealthPath}}$' | grep -v '{' || true)
        if [ -z "$other_paths" ]; then
          echo 'OPENAPI_CHECK_FAILED: no endpoints beyond {{ProjectLayout.HealthPath}} were found in the OpenAPI document.'
          exit 1
        fi
        echo 'OPENAPI_CHECK_PASSED'

        smoke_path=$(echo "$other_paths" | head -n1)
        echo "----- smoke testing $smoke_path -----"
        code=$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:{{ProjectLayout.BackendPort}}$smoke_path" || echo 000)
        echo "Smoke test status: $code"
        if [ "$code" -ge 500 ] || [ "$code" = "000" ]; then
          echo "SMOKE_CHECK_FAILED: GET $smoke_path returned $code"
          exit 1
        fi
        echo 'SMOKE_CHECK_PASSED'
        """;

    /// <summary>
    /// Discovers the generated test projects and runs each one. A project is a
    /// test project when it references the test SDK -- naming conventions vary
    /// too much between generated solutions to rely on. Running each project
    /// separately (rather than the whole solution) keeps the failure output
    /// attributable to a specific suite, which is what the repair prompt needs.
    /// </summary>
    private static readonly string TestScript = """
        set -e
        echo '----- discovering test projects -----'
        test_projects=$(grep -rl 'Microsoft.NET.Test.Sdk' --include='*.csproj' . || true)
        if [ -z "$test_projects" ]; then
          echo 'TEST_CHECK_FAILED: the specification calls for automated tests, but no project referencing'
          echo 'Microsoft.NET.Test.Sdk exists. Add a test project under backend/ that dotnet test discovers,'
          echo 'covering the acceptance criteria in the endpoint, entity and backend specifications.'
          exit 1
        fi
        echo "$test_projects"
        dotnet restore
        echo 'Waiting for the database to accept connections…'
        sleep 10
        failed=0
        for project in $test_projects; do
          echo "----- dotnet test $project -----"
          dotnet test "$project" --nologo --verbosity normal || failed=1
        done
        if [ "$failed" != "0" ]; then
          echo 'TEST_CHECK_FAILED: one or more generated tests failed. Fix the implementation the test'
          echo 'exercises, or the test if it contradicts its specification.'
          exit 1
        fi
        echo 'TEST_CHECK_PASSED'
        """;

    /// <summary>
    /// Counts tables in the public schema from the database's own side, so
    /// "the app booted" and "the app actually created a schema" are checked
    /// independently -- an app with a broken migration can still pass its own
    /// health probe if the endpoint doesn't touch the database.
    /// </summary>
    private static readonly string SchemaCheckScript = $"""
        set -e
        count=$(psql -h {DatabaseAlias} -U {DatabaseUser} -d {DatabaseName} -tAc \
          "select count(*) from information_schema.tables where table_schema='public';")
        echo "Public tables found: $count"
        if [ "$count" -gt 0 ]; then
          echo 'MIGRATIONS_CHECK_PASSED'
          exit 0
        fi
        echo 'MIGRATIONS_CHECK_FAILED: no tables exist in the public schema -- migrations or schema creation did not run.'
        exit 1
        """;
}
