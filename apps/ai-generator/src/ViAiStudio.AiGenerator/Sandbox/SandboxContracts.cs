namespace ViAiStudio.AiGenerator.Sandbox;

/// <summary>
/// Why a sandbox run failed, so callers can tell "the generated code is
/// broken" (route to the model) apart from "the container/daemon/network
/// misbehaved" (retry directly -- asking the model to fix a Docker timeout
/// is nonsensical and wastes a repair attempt).
/// </summary>
public enum SandboxFailureKind
{
    /// <summary>Not a failure, or a plain non-zero exit from the command itself -- a real code problem.</summary>
    CommandFailed,

    /// <summary>The command didn't finish inside its allotted time.</summary>
    Timeout,

    /// <summary>Could not pull the sandbox image (registry hiccup, network blip).</summary>
    ImagePullFailed,

    /// <summary>Could not reach the Docker daemon at all.</summary>
    DaemonUnreachable,

    /// <summary>Could not create/start the container even though the daemon answered.</summary>
    ContainerStartFailed,

    /// <summary>Container was killed for exceeding its memory ceiling (exit code 137).</summary>
    ContainerOom,
}

public sealed class SandboxOptions
{
    /// <summary>
    /// Docker daemon endpoint. Defaults to the platform's local socket --
    /// npipe on Windows, unix socket elsewhere (including this service's own
    /// container when /var/run/docker.sock is mounted in).
    /// </summary>
    public string? DockerEndpoint { get; set; }

    /// <summary>Directory this process writes build workspaces into.</summary>
    public string WorkspaceRoot { get; set; } = Path.Combine(Path.GetTempPath(), "vi-ai-studio-builds");

    /// <summary>
    /// The same directory as the *Docker daemon* sees it. Identical to
    /// <see cref="WorkspaceRoot"/> when running natively, but when this service
    /// runs inside a container the daemon is on the host and cannot resolve
    /// container-local paths -- bind mounts would silently come up empty.
    /// </summary>
    public string? HostWorkspaceRoot { get; set; }

    public string BackendImage { get; set; } = "mcr.microsoft.com/dotnet/sdk:10.0";
    public string FrontendImage { get; set; } = "node:22-alpine";
    public string DatabaseImage { get; set; } = "postgres:16-alpine";

    /// <summary>Ceiling on a single sandbox command; a runaway build must not wedge the queue.</summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>How many repair rounds to attempt before giving up on a failing step.</summary>
    public int MaxRepairAttempts { get; set; } = 4;

    /// <summary>
    /// How many times to retry a step directly -- no model call, no repair
    /// attempt spent -- when it failed for an infrastructure reason rather
    /// than a code reason. Kept small: a real outage should surface, not be
    /// silently absorbed forever.
    /// </summary>
    public int MaxInfraRetries { get; set; } = 2;

    /// <summary>Delay between infra retries, to give a transient blip a chance to clear.</summary>
    public TimeSpan InfraRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Named Docker volume that caches the NuGet global-packages folder across
    /// every backend build/restore, so a repair round -- or the next build
    /// entirely -- doesn't re-download the same packages from a cold container.
    /// </summary>
    public string NugetCacheVolume { get; set; } = "vi-ai-nuget-cache";

    /// <summary>Named Docker volume caching npm's package cache across frontend builds.</summary>
    public string NpmCacheVolume { get; set; } = "vi-ai-npm-cache";
}

public sealed record SandboxRun(
    string Image,
    string ShellCommand,
    string WorkspaceHostPath,
    string? WorkingSubdirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null,
    string? NetworkName = null,
    TimeSpan? Timeout = null,
    /// <summary>Extra bind mounts beyond the workspace, formatted as Docker expects: "source:target".</summary>
    IReadOnlyList<string>? AdditionalBinds = null);

public sealed record SandboxRunResult(
    bool Succeeded, long ExitCode, string Output, SandboxFailureKind FailureKind = SandboxFailureKind.CommandFailed)
{
    public static SandboxRunResult Failed(string output, SandboxFailureKind kind = SandboxFailureKind.CommandFailed) =>
        new(false, -1, output, kind);
}

/// <summary>A throwaway docker network plus any long-running services attached to it.</summary>
public interface ISandboxNetwork : IAsyncDisposable
{
    string Name { get; }

    /// <summary>
    /// Starts a detached container reachable from other containers on this
    /// network as <paramref name="alias"/>. Used for the database the generated
    /// backend has to connect to.
    /// </summary>
    Task StartServiceAsync(string image, string alias, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken);
}

public interface ISandboxExecutor
{
    Task<SandboxRunResult> RunAsync(SandboxRun run, CancellationToken cancellationToken);

    Task<ISandboxNetwork> CreateNetworkAsync(CancellationToken cancellationToken);
}
