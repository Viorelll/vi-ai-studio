namespace ViAiStudio.AiGenerator.Sandbox;

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
}

public sealed record SandboxRun(
    string Image,
    string ShellCommand,
    string WorkspaceHostPath,
    string? WorkingSubdirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null,
    string? NetworkName = null,
    TimeSpan? Timeout = null);

public sealed record SandboxRunResult(bool Succeeded, long ExitCode, string Output)
{
    public static SandboxRunResult Failed(string output) => new(false, -1, output);
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
