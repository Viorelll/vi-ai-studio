using Microsoft.Extensions.Options;
using ViAiStudio.AiGenerator.Callback;
using ViAiStudio.AiGenerator.Contracts;
using ViAiStudio.AiGenerator.Generation;
using ViAiStudio.AiGenerator.Sandbox;
using ViAiStudio.AiGenerator.Storage;

namespace ViAiStudio.AiGenerator.Builds;

/// <summary>
/// Runs one AI Build end to end: the model generates the project from the
/// specification, then every verification step is executed in a Docker
/// sandbox and any failure is handed straight back to the model to fix. The
/// build is only "done" once the backend compiles, the frontend compiles, and
/// the stack boots against a real database -- so the artifact that reaches
/// the user is one that provably ran, not one that merely looked plausible.
/// </summary>
public sealed class BuildOrchestrator(
    ProjectCodeGenerator codeGenerator,
    ProjectVerifier verifier,
    BuildWorkspaceFactory workspaceFactory,
    ApiCallbackClient callbackClient,
    MinioArchiveWriter archiveWriter,
    IBuildJobStore jobStore,
    IOptions<SandboxOptions> sandboxOptions,
    ILogger<BuildOrchestrator> logger)
{
    private readonly SandboxOptions options = sandboxOptions.Value;

    private const int GenerationCompletePct = 25;
    private const int VerificationCompletePct = 92;

    public async Task RunAsync(string jobId, StartBuildRequest request, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var aiCalls = new List<BuildAiCallReport>();
        using var workspace = workspaceFactory.Create(request.GenerationId);

        try
        {
            await GenerateProjectAsync(jobId, request, workspace, aiCalls, cancellationToken);
            await VerifyProjectAsync(jobId, request, workspace, aiCalls, cancellationToken);

            await ReportAsync(jobId, request, "Done", "Packaging the project…", 95, cancellationToken);
            var archiveStorageKey = await archiveWriter.WriteArchiveAsync(
                request.GenerationId, workspace.CreateArchive(), cancellationToken);

            jobStore.Update(jobId, job => { job.Status = BuildJobStatus.Ready; job.ProgressPct = 100; });
            await callbackClient.ReportCompleteAsync(
                request.CallbackBaseUrl, request.GenerationId,
                new CompleteBuildPayload(
                    Success: true,
                    FailureReason: null,
                    Elapsed(startedAt),
                    workspace.FileTree.ToList(),
                    archiveStorageKey,
                    aiCalls),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Build {GenerationId} failed", request.GenerationId);
            jobStore.Update(jobId, job => job.Status = BuildJobStatus.Failed);

            try
            {
                await callbackClient.ReportCompleteAsync(
                    request.CallbackBaseUrl, request.GenerationId,
                    new CompleteBuildPayload(
                        Success: false,
                        ex.Message,
                        Elapsed(startedAt),
                        workspace.FileTree.ToList(),
                        // Ship the archive even on failure: a project that got most of
                        // the way is still worth inspecting, and the file tree the Api
                        // records would otherwise point at nothing downloadable.
                        await TryArchiveAsync(request.GenerationId, workspace),
                        aiCalls),
                    CancellationToken.None);
            }
            catch (Exception callbackEx)
            {
                logger.LogError(callbackEx, "Failed to report build failure for {GenerationId} back to the Api", request.GenerationId);
            }
        }
    }

    private async Task GenerateProjectAsync(
        string jobId, StartBuildRequest request, BuildWorkspace workspace,
        List<BuildAiCallReport> aiCalls, CancellationToken cancellationToken)
    {
        await ReportAsync(jobId, request, "Planning", "Analyzing the specification…", 4, cancellationToken);
        await ReportAsync(jobId, request, "Coding", $"Generating the project with {request.Model}…", 8, cancellationToken);

        var generated = await codeGenerator.GenerateAsync(request, cancellationToken);
        aiCalls.Add(generated.Call);
        workspace.ApplyFiles(generated.Files);

        await ReportAsync(jobId, request, "Scaffolding",
            $"Generated {workspace.FileTree.Count} files.", GenerationCompletePct, cancellationToken);
    }

    /// <summary>
    /// One gate the project has to pass. Wrapping both the static, no-Docker
    /// check and every Docker-backed verification step in the same shape lets
    /// them share one repair loop, one infra-retry policy and one progress
    /// report, instead of static validation needing bespoke plumbing.
    /// </summary>
    private sealed record PipelineStep(string Name, string StartMessage, Func<CancellationToken, Task<SandboxRunResult>> Run);

    private async Task VerifyProjectAsync(
        string jobId, StartBuildRequest request, BuildWorkspace workspace,
        List<BuildAiCallReport> aiCalls, CancellationToken cancellationToken)
    {
        var steps = BuildPipeline(workspace);
        var span = (VerificationCompletePct - GenerationCompletePct) / (double)steps.Count;

        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var stepStartPct = GenerationCompletePct + (int)(span * index);

            await ReportAsync(jobId, request, step.Name, step.StartMessage, stepStartPct, cancellationToken);

            var result = await RunStepWithRepairAsync(jobId, request, workspace, step, aiCalls, stepStartPct, cancellationToken);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{step.Name} still failing after {options.MaxRepairAttempts} repair attempt(s). Last output:\n{Tail(result.Output)}");
            }

            await ReportAsync(jobId, request, step.Name, $"{step.Name} passed ✓",
                GenerationCompletePct + (int)(span * (index + 1)), cancellationToken);
        }
    }

    /// <summary>
    /// The static check runs first and free -- catching a missing project
    /// reference or an unversioned package here costs milliseconds, where the
    /// same mistake surfacing from <c>Backend build</c> costs a full Docker run.
    /// </summary>
    private List<PipelineStep> BuildPipeline(BuildWorkspace workspace)
    {
        var pipeline = new List<PipelineStep>
        {
            new("Static validation", "Checking project structure…",
                _ => Task.FromResult(ProjectStaticValidator.Validate(workspace.Files))),
        };
        pipeline.AddRange(verifier.Steps.Select(step =>
            new PipelineStep(step.Name, step.StartMessage, ct => verifier.RunAsync(step, workspace.HostPath, ct))));
        return pipeline;
    }

    /// <summary>
    /// Runs one step, feeding its failure output back to the model and
    /// re-running until it passes or the attempt budget is spent. Infra
    /// failures (Docker timeouts, image pulls, a daemon blip) are retried
    /// directly and never reach the model or consume a repair attempt --
    /// asking the model to "fix" a container timeout is nonsensical. Also
    /// bails out early if a repair round produces no file changes, or if two
    /// consecutive attempts land on the identical failure signature: a loop
    /// that keeps producing different files but the same error is not making
    /// progress and shouldn't be allowed to burn the rest of its budget.
    /// </summary>
    private async Task<SandboxRunResult> RunStepWithRepairAsync(
        string jobId, StartBuildRequest request, BuildWorkspace workspace, PipelineStep step,
        List<BuildAiCallReport> aiCalls, int stepStartPct, CancellationToken cancellationToken)
    {
        var result = await RunWithInfraRetriesAsync(step, cancellationToken);
        var previousSignature = result.Succeeded ? null : ErrorSignature.Compute(result.Output);

        for (var attempt = 1; attempt <= options.MaxRepairAttempts && !result.Succeeded; attempt++)
        {
            await ReportAsync(jobId, request, step.Name,
                $"{step.Name} failed — asking {request.Model} for a fix (attempt {attempt}/{options.MaxRepairAttempts})…",
                stepStartPct, cancellationToken);

            var repair = await codeGenerator.RepairAsync(
                request, workspace.Files, step.Name, result.Output, cancellationToken);
            aiCalls.Add(repair.Call);

            if (workspace.ApplyFiles(repair.Files) == 0)
            {
                logger.LogWarning("Repair attempt {Attempt} for {Step} changed no files; stopping early", attempt, step.Name);
                break;
            }

            await ReportAsync(jobId, request, step.Name,
                $"Applied {repair.Files.Count} file change(s); re-running {step.Name}…", stepStartPct, cancellationToken);

            result = await RunWithInfraRetriesAsync(step, cancellationToken);
            if (result.Succeeded) break;

            var signature = ErrorSignature.Compute(result.Output);
            if (signature == previousSignature)
            {
                logger.LogWarning(
                    "Repair attempt {Attempt} for {Step} produced the same failure as before; stopping early instead of repeating it",
                    attempt, step.Name);
                break;
            }
            previousSignature = signature;
        }

        return result;
    }

    /// <summary>
    /// Retries a step directly -- no model call, no repair attempt spent --
    /// while it keeps failing for an infrastructure reason. Once it either
    /// succeeds, fails for a genuine code reason, or exhausts its retry
    /// budget, the result is handed back to the repair loop as-is.
    /// </summary>
    private async Task<SandboxRunResult> RunWithInfraRetriesAsync(PipelineStep step, CancellationToken cancellationToken)
    {
        var result = await step.Run(cancellationToken);

        for (var infraAttempt = 1; !result.Succeeded && IsInfraFailure(result.FailureKind) && infraAttempt <= options.MaxInfraRetries; infraAttempt++)
        {
            logger.LogWarning(
                "{Step} failed for an infrastructure reason ({Kind}); retrying directly ({Attempt}/{Max}) without invoking the model",
                step.Name, result.FailureKind, infraAttempt, options.MaxInfraRetries);

            await Task.Delay(options.InfraRetryDelay, cancellationToken);
            result = await step.Run(cancellationToken);
        }

        return result;
    }

    private static bool IsInfraFailure(SandboxFailureKind kind) => kind is
        SandboxFailureKind.Timeout or
        SandboxFailureKind.ImagePullFailed or
        SandboxFailureKind.DaemonUnreachable or
        SandboxFailureKind.ContainerStartFailed or
        SandboxFailureKind.ContainerOom;

    private async Task<string?> TryArchiveAsync(Guid generationId, BuildWorkspace workspace)
    {
        if (workspace.FileTree.Count == 0) return null;

        try
        {
            return await archiveWriter.WriteArchiveAsync(generationId, workspace.CreateArchive(), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not archive the partial project for {GenerationId}", generationId);
            return null;
        }
    }

    private async Task ReportAsync(
        string jobId, StartBuildRequest request, string stepLabel, string logLine, int progressPct, CancellationToken cancellationToken)
    {
        jobStore.Update(jobId, job =>
        {
            job.CurrentStep = stepLabel;
            job.ProgressPct = progressPct;
        });

        try
        {
            await callbackClient.ReportProgressAsync(
                request.CallbackBaseUrl, request.GenerationId, stepLabel, logLine, progressPct, cancellationToken);
        }
        catch (Exception ex)
        {
            // Progress is telemetry: losing a line must not abort a build that is
            // otherwise fine. The terminal complete/fail callback is what matters.
            logger.LogWarning(ex, "Could not report progress for {GenerationId}", request.GenerationId);
        }
    }

    private static int Elapsed(DateTimeOffset startedAt) => (int)(DateTimeOffset.UtcNow - startedAt).TotalSeconds;

    private static string Tail(string output, int max = 4000) =>
        output.Length <= max ? output : output[^max..];
}
