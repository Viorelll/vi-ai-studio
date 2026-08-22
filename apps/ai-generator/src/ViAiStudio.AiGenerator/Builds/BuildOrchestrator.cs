using Microsoft.Extensions.Options;
using ViAiStudio.AiGenerator.Callback;
using ViAiStudio.AiGenerator.Contracts;
using ViAiStudio.AiGenerator.Generation;
using ViAiStudio.AiGenerator.Sandbox;
using ViAiStudio.AiGenerator.Storage;

namespace ViAiStudio.AiGenerator.Builds;

/// <summary>
/// A verification step that is still failing after its repair budget. Typed
/// so the orchestrator can tell "this step didn't pass" -- which is worth
/// another round of the whole pipeline -- apart from a genuine fault such as
/// an unreachable model or a broken workspace, which retrying cannot help.
/// </summary>
public sealed class BuildStepFailedException(string stepName, string message) : Exception(message)
{
    public string StepName { get; } = stepName;
}

/// <summary>
/// Runs one AI Build end to end.
///
/// The specification is generated in ordered phases -- a real specification
/// runs to hundreds of thousands of characters across a hundred-plus
/// documents, which no model can implement in a single reply -- then every
/// specification is checked against the resulting project, any that were
/// missed are filled in, and only then is the project compiled, tested and
/// booted in a Docker sandbox, with each failure handed straight back to the
/// model to fix. The build is only "done" once every specification is
/// accounted for, the backend and frontend compile, the generated tests pass,
/// and the stack boots against a real database -- so the artifact that reaches
/// the user provably ran and provably implements what was specified, rather
/// than merely looking plausible.
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

    /// <summary>
    /// Stable stage labels. The browser's build timeline is driven by these,
    /// so they are a small fixed vocabulary -- the detail of what is happening
    /// belongs in the log line, which is free-form. A stage label the UI has
    /// never heard of would leave its timeline stuck on the previous stage.
    /// </summary>
    private const string StagePlanning = "Planning";
    private const string StageGenerating = "Generating";
    private const string StageCoverage = "Coverage";
    private const string StageVerifying = "Verifying";
    private const string StageDone = "Done";

    /// <summary>
    /// Ceiling on how many missing specifications one gap-filling call is
    /// asked to implement. Deliberately small: the reason single-shot
    /// generation failed was the output limit, and a gap-filling call that has
    /// to write forty specifications' worth of code hits the same wall.
    /// </summary>
    private const int MaxSpecsPerCoverageCall = 6;

    private const int PlanningCompletePct = 5;
    private const int GenerationCompletePct = 55;
    private const int CoverageCompletePct = 68;
    private const int VerificationCompletePct = 93;

    public async Task RunAsync(string jobId, StartBuildRequest request, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var aiCalls = new List<BuildAiCallReport>();
        using var workspace = workspaceFactory.Create(request.GenerationId);

        try
        {
            var plan = await PlanBuildAsync(jobId, request, cancellationToken);

            await GeneratePhasesAsync(jobId, request, plan, workspace, aiCalls, cancellationToken);
            await CompleteProjectAsync(jobId, request, plan, workspace, aiCalls, cancellationToken);

            // The specification is re-checked against the finished project:
            // the verification steps rewrite files to fix compile and test
            // failures, so the coverage measured before they ran is no longer
            // a statement about what is being shipped. This is the one place
            // the coverage threshold is enforced -- against what will actually
            // be shipped, not against an intermediate state.
            var finalCoverage = SpecCoverageValidator.Analyze(request.Documents, workspace.Files);
            await ReportAsync(jobId, request, StageVerifying,
                $"Final specification check: {finalCoverage.Summary}.", VerificationCompletePct, cancellationToken);

            if (finalCoverage.PercentComplete < options.MinimumSpecCoveragePct)
            {
                throw new InvalidOperationException(
                    $"The project builds, but specification coverage is {finalCoverage.PercentComplete}%, below the "
                    + $"{options.MinimumSpecCoveragePct}% required for a build to be considered done.\n{finalCoverage.DescribeGaps()}");
            }

            await ReportAsync(jobId, request, StageDone,
                $"Packaging {workspace.FileTree.Count} files…", 95, cancellationToken);
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

    /// <summary>What this build intends to do, reported to the user before any model call is made.</summary>
    private sealed record BuildPlan(
        IReadOnlyList<BuildPhase> Phases,
        IReadOnlyList<SpecDocumentDto> SharedContext,
        bool RequiresTests);

    /// <summary>
    /// Works out the phases up front and narrates the plan, so the log opens
    /// with what is about to happen and how much of it there is -- a build
    /// that silently thinks for several minutes looks indistinguishable from
    /// one that has hung.
    /// </summary>
    private async Task<BuildPlan> PlanBuildAsync(string jobId, StartBuildRequest request, CancellationToken cancellationToken)
    {
        await ReportAsync(jobId, request, StagePlanning, "Reading the specification…", 2, cancellationToken);

        var documents = request.Documents;
        var phases = ProjectBuildPlanner.Plan(documents);
        var sharedContext = ProjectBuildPlanner.SelectSharedContext(documents);
        var codeBearing = documents.Count(ProjectBuildPlanner.IsCodeBearing);
        var totalCharacters = documents.Sum(d => d.Content.Length);

        if (phases.Count == 0)
        {
            throw new InvalidOperationException(
                "The specification contains no documents to implement. Generate the specification before starting AI Build.");
        }

        await ReportAsync(jobId, request, StagePlanning,
            $"Specification: {documents.Count} document(s), {totalCharacters:N0} characters — "
            + $"{codeBearing} to implement, {documents.Count - codeBearing} background/reference.",
            3, cancellationToken);

        await ReportAsync(jobId, request, StagePlanning,
            $"Planned {phases.Count} generation phase(s). Each phase implements its own specifications and builds "
            + "on the files the previous phases wrote:",
            4, cancellationToken);

        foreach (var phase in phases)
        {
            await ReportAsync(jobId, request, StagePlanning,
                $"  {phase.Index}. {phase.Name} — {phase.Specs.Count} spec(s): {phase.SpecIdRange}",
                4, cancellationToken);
        }

        var requiresTests = phases.Any(p => p.Name == "Quality and tests");
        await ReportAsync(jobId, request, StagePlanning,
            requiresTests
                ? "The specification calls for automated tests, so the generated test suite will be run as a build step."
                : "The specification defines no test requirements, so no test step will run.",
            PlanningCompletePct, cancellationToken);

        return new BuildPlan(phases, sharedContext, requiresTests);
    }

    /// <summary>
    /// Generates the project one phase at a time. Each phase is an independent
    /// model call carrying its own specifications in full plus the list of
    /// files written so far, and the workspace accumulates the result -- which
    /// is what allows a specification far larger than any single model reply to
    /// be implemented completely.
    /// </summary>
    private async Task GeneratePhasesAsync(
        string jobId, StartBuildRequest request, BuildPlan plan, BuildWorkspace workspace,
        List<BuildAiCallReport> aiCalls, CancellationToken cancellationToken)
    {
        var span = (GenerationCompletePct - PlanningCompletePct) / (double)plan.Phases.Count;

        var failedPhases = new List<string>();

        foreach (var phase in plan.Phases)
        {
            var phaseStartPct = PlanningCompletePct + (int)(span * (phase.Index - 1));

            await ReportAsync(jobId, request, StageGenerating,
                $"Phase {phase.Index}/{plan.Phases.Count} — {phase.Name}: implementing {phase.Specs.Count} "
                + $"specification(s) ({phase.SpecCharacters:N0} chars) with {request.Model}…",
                phaseStartPct, cancellationToken);

            // A phase's reply can be unusable -- models truncate large JSON
            // bodies, and a half-written object parses as nothing. Retrying is
            // almost always enough; carrying on when it isn't matters more,
            // because the coverage pass will notice this phase's specifications
            // are unimplemented and fill them in. Aborting here would throw away
            // every phase that already succeeded.
            for (var attempt = 1; attempt <= options.MaxPhaseAttempts; attempt++)
            {
                try
                {
                    var generated = await codeGenerator.GeneratePhaseAsync(
                        request, phase, plan.SharedContext, workspace.Files, cancellationToken);
                    aiCalls.Add(generated.Call);

                    var before = workspace.FileTree.Count;
                    var changed = workspace.ApplyFiles(generated.Files);
                    var added = workspace.FileTree.Count - before;

                    await ReportAsync(jobId, request, StageGenerating,
                        $"Phase {phase.Index}/{plan.Phases.Count} — {phase.Name}: {added} new file(s), "
                        + $"{changed - added} updated. Project now has {workspace.FileTree.Count} file(s).",
                        PlanningCompletePct + (int)(span * phase.Index), cancellationToken);
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Phase {Phase} attempt {Attempt} failed", phase.Name, attempt);

                    if (attempt < options.MaxPhaseAttempts)
                    {
                        await ReportAsync(jobId, request, StageGenerating,
                            $"Phase {phase.Index}/{plan.Phases.Count} — {phase.Name}: the model's reply could not "
                            + $"be used ({ex.Message}). Retrying ({attempt + 1}/{options.MaxPhaseAttempts})…",
                            phaseStartPct, cancellationToken);
                        continue;
                    }

                    failedPhases.Add(phase.Name);
                    await ReportAsync(jobId, request, StageGenerating,
                        $"Phase {phase.Index}/{plan.Phases.Count} — {phase.Name}: still unusable after "
                        + $"{options.MaxPhaseAttempts} attempts. Continuing; the coverage step will pick up its "
                        + "specifications.",
                        PlanningCompletePct + (int)(span * phase.Index), cancellationToken);
                }
            }
        }

        if (workspace.FileTree.Count == 0)
        {
            throw new InvalidOperationException(
                "No phase produced a usable reply, so there is no project to verify. "
                + $"Failed phases: {string.Join(", ", failedPhases)}.");
        }

        await ReportAsync(jobId, request, StageGenerating,
            failedPhases.Count == 0
                ? $"All {plan.Phases.Count} phase(s) generated — {workspace.FileTree.Count} file(s) in total."
                : $"{plan.Phases.Count - failedPhases.Count}/{plan.Phases.Count} phase(s) generated — "
                  + $"{workspace.FileTree.Count} file(s). Incomplete: {string.Join(", ", failedPhases)}.",
            GenerationCompletePct, cancellationToken);
    }

    /// <summary>
    /// Drives the project from "generated" to "actually works", re-running the
    /// whole fill-coverage-then-verify pipeline until it passes.
    ///
    /// A single pass gives each step one repair budget and gives up when it is
    /// spent, which is what made a build stop while it was still close. A
    /// round costs time but nothing else: every round starts from the files
    /// the previous one left behind, so the work accumulates -- a project that
    /// failed the frontend build in round 1 usually has a compiling frontend
    /// and a further-along backend by round 2.
    /// </summary>
    private async Task CompleteProjectAsync(
        string jobId, StartBuildRequest request, BuildPlan plan, BuildWorkspace workspace,
        List<BuildAiCallReport> aiCalls, CancellationToken cancellationToken)
    {
        for (var round = 1; round <= options.MaxBuildRounds; round++)
        {
            if (round > 1)
            {
                await ReportAsync(jobId, request, StageVerifying,
                    $"── Build round {round}/{options.MaxBuildRounds} — re-checking coverage and re-running every "
                    + $"verification step against the {workspace.FileTree.Count} file(s) so far ──",
                    CoverageCompletePct, cancellationToken);
            }

            await EnsureSpecificationCoverageAsync(jobId, request, workspace, aiCalls, cancellationToken);

            try
            {
                await VerifyProjectAsync(jobId, request, plan, workspace, aiCalls, cancellationToken);
                return;
            }
            catch (BuildStepFailedException ex) when (round < options.MaxBuildRounds)
            {
                logger.LogWarning("Build round {Round} did not complete: {Step} still failing", round, ex.StepName);
                await ReportAsync(jobId, request, StageVerifying,
                    $"Round {round}/{options.MaxBuildRounds} ended with {ex.StepName} still failing. "
                    + "Starting another round rather than giving up — the fixes made so far are kept.",
                    CoverageCompletePct, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Checks every specification against the generated project and asks the
    /// model to implement whatever is missing.
    ///
    /// Phased generation makes complete coverage possible; this is what makes
    /// it verifiable. A model can quietly skip a specification inside a phase,
    /// and nothing downstream would notice -- the project would still compile
    /// and boot. The loop stops when coverage is complete, when a round stops
    /// closing gaps, or when the attempt budget runs out.
    /// </summary>
    private async Task EnsureSpecificationCoverageAsync(
        string jobId, StartBuildRequest request, BuildWorkspace workspace,
        List<BuildAiCallReport> aiCalls, CancellationToken cancellationToken)
    {
        await ReportAsync(jobId, request, StageCoverage,
            "Checking every specification against the generated project…", GenerationCompletePct + 1, cancellationToken);

        var report = SpecCoverageValidator.Analyze(request.Documents, workspace.Files);
        await ReportAsync(jobId, request, StageCoverage,
            $"Specification coverage: {report.Summary}.", GenerationCompletePct + 3, cancellationToken);

        // How many missing specifications to ask for per call, halved whenever
        // a round closes nothing (see below).
        var batchSize = MaxSpecsPerCoverageCall;

        for (var attempt = 1; attempt <= options.MaxCoverageAttempts && report.Missing.Count > 0; attempt++)
        {
            await ReportAsync(jobId, request, StageCoverage,
                $"{report.Missing.Count} specification(s) have no implementation yet — asking {request.Model} "
                + $"to complete them (attempt {attempt}/{options.MaxCoverageAttempts})…",
                GenerationCompletePct + 3, cancellationToken);

            foreach (var gap in report.Missing.Take(12))
            {
                await ReportAsync(jobId, request, StageCoverage,
                    $"  • {(string.IsNullOrWhiteSpace(gap.Spec.SpecId) ? gap.Spec.Path : gap.Spec.SpecId)} "
                    + $"“{gap.Spec.Title}” — {gap.Evidence}",
                    GenerationCompletePct + 3, cancellationToken);
            }

            // Asked for in chunks, not all at once. A gap-filling call that has
            // to implement forty specifications in one reply hits exactly the
            // output ceiling that made single-shot generation fail in the first
            // place, and comes back truncated or shallow.
            var missingSpecs = report.Missing.Select(m => m.Spec).ToList();
            var applied = 0;
            var chunks = missingSpecs.Chunk(batchSize).ToList();

            for (var chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                if (chunks.Count > 1)
                {
                    await ReportAsync(jobId, request, StageCoverage,
                        $"  implementing group {chunkIndex + 1}/{chunks.Count} ({chunk.Length} specification(s))…",
                        GenerationCompletePct + 4, cancellationToken);
                }

                try
                {
                    var filled = await codeGenerator.ImplementMissingAsync(
                        request, chunk, workspace.Files, cancellationToken);
                    aiCalls.Add(filled.Call);
                    applied += workspace.ApplyFiles(filled.Files);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One unusable reply must not abandon the other groups.
                    logger.LogWarning(ex, "Coverage group {Group} produced an unusable reply", chunkIndex + 1);
                    await ReportAsync(jobId, request, StageCoverage,
                        $"  group {chunkIndex + 1}/{chunks.Count} came back unusable ({ex.Message}); continuing…",
                        GenerationCompletePct + 4, cancellationToken);
                }
            }

            await ReportAsync(jobId, request, StageCoverage,
                $"Applied {applied} file change(s); re-checking coverage…",
                GenerationCompletePct + 5, cancellationToken);

            var previouslyMissing = report.Missing.Count;
            report = SpecCoverageValidator.Analyze(request.Documents, workspace.Files);

            await ReportAsync(jobId, request, StageCoverage,
                $"Specification coverage: {report.Summary}.", GenerationCompletePct + 6, cancellationToken);

            if (report.Missing.Count >= previouslyMissing)
            {
                // No gaps closed. Asking the same way again would get the same
                // answer, so narrow the ask instead -- a smaller group is a
                // materially easier request, and often succeeds where a large
                // one returned something shallow.
                if (batchSize <= 1)
                {
                    logger.LogWarning(
                        "Coverage attempt {Attempt} closed no gaps at the smallest group size; leaving the rest to the next build round",
                        attempt);
                    await ReportAsync(jobId, request, StageCoverage,
                        "No further gaps could be closed this round; the next build round will try again.",
                        GenerationCompletePct + 6, cancellationToken);
                    break;
                }

                batchSize = Math.Max(1, batchSize / 2);
                await ReportAsync(jobId, request, StageCoverage,
                    $"That round closed no gaps — retrying in smaller groups of {batchSize}…",
                    GenerationCompletePct + 6, cancellationToken);
            }
        }

        // Deliberately does not enforce the threshold. Verification rewrites
        // files to fix compile and test failures, and every build round comes
        // back through here, so the only measurement worth failing a build over
        // is the one taken against the finished project (see RunAsync).
        await ReportAsync(jobId, request, StageCoverage,
            report.Missing.Count == 0
                ? $"Specification coverage complete — {report.Summary} ✓"
                : $"{report.Summary} — {report.PercentComplete}% accounted for. "
                  + "Any specifications still missing are listed above.",
            CoverageCompletePct, cancellationToken);
    }

    /// <summary>
    /// One gate the project has to pass. Wrapping both the static, no-Docker
    /// check and every Docker-backed verification step in the same shape lets
    /// them share one repair loop, one infra-retry policy and one progress
    /// report, instead of static validation needing bespoke plumbing.
    /// </summary>
    private sealed record PipelineStep(string Name, string StartMessage, Func<CancellationToken, Task<SandboxRunResult>> Run);

    private async Task VerifyProjectAsync(
        string jobId, StartBuildRequest request, BuildPlan plan, BuildWorkspace workspace,
        List<BuildAiCallReport> aiCalls, CancellationToken cancellationToken)
    {
        var steps = BuildPipeline(plan, workspace);
        var span = (VerificationCompletePct - CoverageCompletePct) / (double)steps.Count;

        await ReportAsync(jobId, request, StageVerifying,
            $"Verifying the project in {steps.Count} step(s): {string.Join(" → ", steps.Select(s => s.Name))}.",
            CoverageCompletePct, cancellationToken);

        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var stepStartPct = CoverageCompletePct + (int)(span * index);

            await ReportAsync(jobId, request, StageVerifying,
                $"Step {index + 1}/{steps.Count} — {step.Name}: {step.StartMessage}", stepStartPct, cancellationToken);

            var result = await RunStepWithRepairAsync(jobId, request, workspace, step, aiCalls, stepStartPct, cancellationToken);
            if (!result.Succeeded)
            {
                throw new BuildStepFailedException(step.Name,
                    $"{step.Name} still failing after {options.MaxRepairAttempts} repair attempt(s). Last output:\n{Tail(result.Output)}");
            }

            await ReportAsync(jobId, request, StageVerifying, $"{step.Name} passed ✓",
                CoverageCompletePct + (int)(span * (index + 1)), cancellationToken);
        }
    }

    /// <summary>
    /// The static check runs first and free -- catching a missing project
    /// reference or an unversioned package here costs milliseconds, where the
    /// same mistake surfacing from <c>Backend build</c> costs a full Docker run.
    /// The test step is included only when the specification asked for tests,
    /// so a project whose specification never mentions testing isn't failed
    /// for lacking a suite nobody specified.
    /// </summary>
    private List<PipelineStep> BuildPipeline(BuildPlan plan, BuildWorkspace workspace)
    {
        var pipeline = new List<PipelineStep>
        {
            new("Static validation", "Checking project structure…",
                _ => Task.FromResult(ProjectStaticValidator.Validate(workspace.Files))),
        };

        var steps = plan.RequiresTests
            ? new[] { ProjectVerifier.BackendBuild, ProjectVerifier.FrontendBuild, ProjectVerifier.AutomatedTests, ProjectVerifier.IntegrationRun }
            : [.. verifier.Steps];

        pipeline.AddRange(steps.Select(step =>
            new PipelineStep(step.Name, step.StartMessage, ct => verifier.RunAsync(step, workspace.HostPath, ct))));
        return pipeline;
    }

    /// <summary>
    /// Runs one step, feeding its failure output back to the model and
    /// re-running until it passes or the attempt budget is spent. Infra
    /// failures (Docker timeouts, image pulls, a daemon blip) are retried
    /// directly and never reach the model or consume a repair attempt --
    /// asking the model to "fix" a container timeout is nonsensical.
    ///
    /// When a round makes no progress -- the same failure signature comes
    /// back, or the model returns nothing to apply -- the next attempt
    /// escalates to sending the whole repository instead of only the files the
    /// diagnostics named, on the reasoning that a failure which survives a
    /// narrowed fix is usually caused by a file the log never mentioned.
    /// Escalating spends the remaining budget rather than surrendering it:
    /// stopping at the first repeated error was the single biggest reason a
    /// build gave up while it still had attempts left.
    /// </summary>
    private async Task<SandboxRunResult> RunStepWithRepairAsync(
        string jobId, StartBuildRequest request, BuildWorkspace workspace, PipelineStep step,
        List<BuildAiCallReport> aiCalls, int stepStartPct, CancellationToken cancellationToken)
    {
        var result = await RunWithInfraRetriesAsync(step, cancellationToken);
        var previousSignature = result.Succeeded ? null : ErrorSignature.Compute(result.Output);
        var escalate = false;
        var barrenRounds = 0;

        for (var attempt = 1; attempt <= options.MaxRepairAttempts && !result.Succeeded; attempt++)
        {
            await ReportAsync(jobId, request, StageVerifying,
                $"{step.Name} failed — asking {request.Model} for a fix (attempt {attempt}/{options.MaxRepairAttempts})"
                + (escalate ? ", this time with the whole repository for context…" : "…"),
                stepStartPct, cancellationToken);

            var repair = await codeGenerator.RepairAsync(
                request, workspace.Files, step.Name, result.Output, escalate, cancellationToken);
            aiCalls.Add(repair.Call);

            if (workspace.ApplyFiles(repair.Files) == 0)
            {
                barrenRounds++;
                logger.LogWarning(
                    "Repair attempt {Attempt} for {Step} changed no files (barren round {Barren})", attempt, step.Name, barrenRounds);

                // Twice in a row with nothing to apply means asking again the
                // same way is pointless -- but escalating changes the question,
                // so it is worth one more round before giving the step up.
                if (barrenRounds >= 2 && escalate) break;

                escalate = true;
                await ReportAsync(jobId, request, StageVerifying,
                    $"The model returned no usable changes for {step.Name}; retrying with the whole repository…",
                    stepStartPct, cancellationToken);
                continue;
            }

            barrenRounds = 0;
            await ReportAsync(jobId, request, StageVerifying,
                $"Applied {repair.Files.Count} file change(s); re-running {step.Name}…", stepStartPct, cancellationToken);

            result = await RunWithInfraRetriesAsync(step, cancellationToken);
            if (result.Succeeded) break;

            var signature = ErrorSignature.Compute(result.Output);
            if (signature == previousSignature)
            {
                logger.LogWarning(
                    "Repair attempt {Attempt} for {Step} produced the same failure as before; escalating to full-project context",
                    attempt, step.Name);
                await ReportAsync(jobId, request, StageVerifying,
                    $"{step.Name} failed the same way again — widening the search to the whole repository…",
                    stepStartPct, cancellationToken);
                escalate = true;
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
