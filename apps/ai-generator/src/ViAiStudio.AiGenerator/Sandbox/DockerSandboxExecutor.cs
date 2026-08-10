using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;

namespace ViAiStudio.AiGenerator.Sandbox;

/// <summary>
/// Runs generated code inside throwaway Docker containers. Generated code is
/// untrusted by construction -- it is whatever a model invented -- so it is
/// never compiled or executed in this process; every build and run happens in
/// a container with no network access to the host, bind-mounted read-write
/// only onto that build's own workspace directory.
/// </summary>
public sealed class DockerSandboxExecutor(
    IOptions<SandboxOptions> options,
    ILogger<DockerSandboxExecutor> logger) : ISandboxExecutor, IDisposable
{
    private readonly DockerClient client = CreateClient(options.Value);
    private readonly SandboxOptions options = options.Value;
    private readonly ILogger<DockerSandboxExecutor> logger = logger;

    private const string WorkspaceMountPath = "/workspace";

    private static DockerClient CreateClient(SandboxOptions options)
    {
        var endpoint = options.DockerEndpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = OperatingSystem.IsWindows() ? "npipe://./pipe/docker_engine" : "unix:///var/run/docker.sock";
        }
        return new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
    }

    public async Task<SandboxRunResult> RunAsync(SandboxRun run, CancellationToken cancellationToken)
    {
        await EnsureImageAsync(run.Image, cancellationToken);

        var workingDir = string.IsNullOrWhiteSpace(run.WorkingSubdirectory)
            ? WorkspaceMountPath
            : $"{WorkspaceMountPath}/{run.WorkingSubdirectory.Replace('\\', '/').Trim('/')}";

        var container = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = run.Image,
            Cmd = ["/bin/sh", "-c", run.ShellCommand],
            WorkingDir = workingDir,
            Env = (run.Environment ?? new Dictionary<string, string>()).Select(kv => $"{kv.Key}={kv.Value}").ToList(),
            HostConfig = new HostConfig
            {
                Binds = [$"{run.WorkspaceHostPath}:{WorkspaceMountPath}"],
                NetworkMode = run.NetworkName,
                // Generated code gets a hard ceiling rather than the host's full
                // resources -- a runaway build shouldn't take the machine down.
                Memory = 4L * 1024 * 1024 * 1024,
            },
        }, cancellationToken);

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(run.Timeout ?? options.CommandTimeout);

            await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), cancellationToken);

            long exitCode;
            try
            {
                var wait = await client.Containers.WaitContainerAsync(container.ID, timeoutSource.Token);
                exitCode = wait.StatusCode;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timed out rather than the whole build being cancelled: stop the
                // container so its logs so far are still readable and reportable.
                await SafeKillAsync(container.ID);
                var timedOutLog = await ReadLogsAsync(container.ID, CancellationToken.None);
                return new SandboxRunResult(false, -1,
                    $"{timedOutLog}\n\nTimed out after {(run.Timeout ?? options.CommandTimeout).TotalMinutes:0.#} minutes.");
            }

            var output = await ReadLogsAsync(container.ID, cancellationToken);
            return new SandboxRunResult(exitCode == 0, exitCode, output);
        }
        finally
        {
            await SafeRemoveContainerAsync(container.ID);
        }
    }

    public async Task<ISandboxNetwork> CreateNetworkAsync(CancellationToken cancellationToken)
    {
        var name = $"vi-ai-build-{Guid.NewGuid():n}";
        var response = await client.Networks.CreateNetworkAsync(new NetworksCreateParameters { Name = name }, cancellationToken);
        return new DockerSandboxNetwork(client, response.ID, name, this);
    }

    internal async Task EnsureImageAsync(string image, CancellationToken cancellationToken)
    {
        try
        {
            await client.Images.InspectImageAsync(image, cancellationToken);
            return;
        }
        catch (DockerImageNotFoundException)
        {
            logger.LogInformation("Pulling sandbox image {Image}", image);
        }

        var (repository, tag) = SplitImage(image);
        await client.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = repository, Tag = tag },
            authConfig: null,
            new Progress<JSONMessage>(),
            cancellationToken);
    }

    private static (string Repository, string Tag) SplitImage(string image)
    {
        // Only split on a tag separator in the last path segment: a registry host
        // may carry a port ("registry:5000/img"), which is not a tag.
        var lastSegment = image.LastIndexOf('/');
        var separator = image.LastIndexOf(':');
        return separator > lastSegment
            ? (image[..separator], image[(separator + 1)..])
            : (image, "latest");
    }

    private async Task<string> ReadLogsAsync(string containerId, CancellationToken cancellationToken)
    {
        using var stream = await client.Containers.GetContainerLogsAsync(
            containerId,
            tty: false,
            new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Timestamps = false },
            cancellationToken);

        var (stdout, stderr) = await stream.ReadOutputToEndAsync(cancellationToken);

        var combined = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(stdout)) combined.Append(stdout);
        if (!string.IsNullOrWhiteSpace(stderr)) combined.AppendLine().Append(stderr);
        return combined.ToString();
    }

    private async Task SafeKillAsync(string containerId)
    {
        try
        {
            await client.Containers.KillContainerAsync(containerId, new ContainerKillParameters());
        }
        catch (DockerApiException ex)
        {
            logger.LogDebug(ex, "Could not kill sandbox container {ContainerId}", containerId);
        }
    }

    internal async Task SafeRemoveContainerAsync(string containerId)
    {
        try
        {
            await client.Containers.RemoveContainerAsync(
                containerId, new ContainerRemoveParameters { Force = true, RemoveVolumes = true });
        }
        catch (DockerApiException ex)
        {
            logger.LogWarning(ex, "Could not remove sandbox container {ContainerId}", containerId);
        }
    }

    public void Dispose() => client.Dispose();

    private sealed class DockerSandboxNetwork(
        DockerClient client, string networkId, string name, DockerSandboxExecutor owner) : ISandboxNetwork
    {
        private readonly List<string> serviceContainerIds = [];

        public string Name => name;

        public async Task StartServiceAsync(
            string image, string alias, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
        {
            await owner.EnsureImageAsync(image, cancellationToken);

            var container = await client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Env = environment.Select(kv => $"{kv.Key}={kv.Value}").ToList(),
                HostConfig = new HostConfig { NetworkMode = name },
                NetworkingConfig = new NetworkingConfig
                {
                    EndpointsConfig = new Dictionary<string, EndpointSettings>
                    {
                        [name] = new EndpointSettings { Aliases = [alias] },
                    },
                },
            }, cancellationToken);

            serviceContainerIds.Add(container.ID);
            await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var containerId in serviceContainerIds)
            {
                await owner.SafeRemoveContainerAsync(containerId);
            }

            try
            {
                await client.Networks.DeleteNetworkAsync(networkId);
            }
            catch (DockerApiException ex)
            {
                owner.logger.LogWarning(ex, "Could not remove sandbox network {Network}", name);
            }
        }
    }
}
