using System.Text;
using System.Text.Json;
using ViAiStudio.AiGenerator.Callback;
using ViAiStudio.AiGenerator.Contracts;
using ViAiStudio.AiGenerator.Providers;

namespace ViAiStudio.AiGenerator.Generation;

public sealed record GeneratedFile(string Path, string Content);

/// <summary>Files the model produced, plus the call record reported back to the Api's audit log.</summary>
public sealed record CodeGenerationResult(IReadOnlyList<GeneratedFile> Files, BuildAiCallReport Call);

/// <summary>
/// Turns a specification into an actual project, and -- when the sandbox
/// rejects it -- turns build errors back into fixes. Both directions go
/// through the same JSON file-list protocol, so a repair is just another
/// generation whose prompt happens to include the failing logs.
/// </summary>
public sealed class ProjectCodeGenerator(IModelProvider modelProvider, ILogger<ProjectCodeGenerator> logger)
{
    private static readonly string SystemPrompt = $$"""
        You are a senior engineer who builds complete, immediately runnable software projects.
        You always reply with a single JSON object and nothing else -- no prose, no markdown fences.
        The JSON shape is exactly: {"files":[{"path":"relative/path.ext","content":"full file contents"}]}
        Paths are repository-relative and use forward slashes.

        You work on one repository across several requests: a phase of new work, or a fix for a
        failing build step. Each request tells you which files already exist. Return only the files
        that request calls for, always complete and compilable -- the caller overlays them onto the
        repository, so a partial file silently destroys the version it replaces.

        The finished repository (not necessarily any single reply) must satisfy this contract:

        {{ProjectLayout.Contract}}
        """;

    /// <summary>
    /// Generates the files for one phase of the build. The model sees this
    /// phase's specifications in full, the product context, and an index of
    /// what earlier phases already wrote -- paths only, never their contents,
    /// so the prompt stays bounded by the phase rather than growing with the
    /// project. Only this phase's files come back; the workspace overlays them
    /// onto what already exists.
    /// </summary>
    public async Task<CodeGenerationResult> GeneratePhaseAsync(
        StartBuildRequest request,
        BuildPhase phase,
        IReadOnlyList<SpecDocumentDto> sharedContext,
        IReadOnlyList<GeneratedFile> existingFiles,
        CancellationToken cancellationToken)
    {
        var prompt = BuildPhasePrompt(request, phase, sharedContext, existingFiles);
        return await CallAsync(request, prompt, cancellationToken);
    }

    /// <summary>
    /// Implements specifications that the coverage check found no trace of.
    /// Distinct from <see cref="RepairAsync"/>: nothing is broken here, the
    /// work was simply never done, so the prompt carries the missing
    /// specifications in full rather than a failure log.
    /// </summary>
    public async Task<CodeGenerationResult> ImplementMissingAsync(
        StartBuildRequest request,
        IReadOnlyList<SpecDocumentDto> missingSpecs,
        IReadOnlyList<GeneratedFile> existingFiles,
        CancellationToken cancellationToken)
    {
        var prompt = BuildGapFillingPrompt(request, missingSpecs, existingFiles);
        return await CallAsync(request, prompt, cancellationToken);
    }

    /// <summary>
    /// Asks for a fix for one failed verification step. Only the files the model
    /// wants to change come back; the caller overlays them onto the project, so
    /// the model never has to re-emit the whole repository to fix one compile error.
    /// </summary>
    /// <param name="escalate">
    /// Send the whole repository rather than only the files the diagnostics
    /// name. Used once a narrowed repair has stopped making progress: when the
    /// same failure keeps coming back, the cause is usually in a file the log
    /// never mentioned, so the narrowing is hiding it.
    /// </param>
    public async Task<CodeGenerationResult> RepairAsync(
        StartBuildRequest request,
        IReadOnlyList<GeneratedFile> currentFiles,
        string stepName,
        string errorLog,
        bool escalate,
        CancellationToken cancellationToken)
    {
        var prompt = BuildRepairPrompt(request, currentFiles, stepName, errorLog, escalate);
        return await CallAsync(request, prompt, cancellationToken);
    }

    private async Task<CodeGenerationResult> CallAsync(StartBuildRequest request, string prompt, CancellationToken cancellationToken)
    {
        var result = await modelProvider.GenerateAsync(
            new ModelRequest(request.Provider, request.Model, request.BaseUrl, request.ApiKey, SystemPrompt, prompt),
            cancellationToken);

        var files = ParseFiles(result.Text);
        logger.LogInformation("Model returned {FileCount} file(s) for generation {GenerationId}", files.Count, request.GenerationId);

        return new CodeGenerationResult(
            files,
            new BuildAiCallReport(request.Model, result.TokensIn, result.TokensOut, prompt, result.Text));
    }

    /// <summary>
    /// The brief shared by every phase: what the product is and which stack it
    /// must be built on. Repeated per phase because each phase is an
    /// independent model call with no memory of the previous ones.
    /// </summary>
    private static void AppendProjectBrief(StringBuilder sb, StartBuildRequest request)
    {
        var stack = request.Stack;
        sb.AppendLine($"Project name: {request.SpecificationName}");
        if (!string.IsNullOrWhiteSpace(request.Summary)) sb.AppendLine($"Summary: {request.Summary}");
        if (!string.IsNullOrWhiteSpace(request.Description)) sb.AppendLine($"Description: {request.Description}");
        if (!string.IsNullOrWhiteSpace(request.Audience)) sb.AppendLine($"Audience: {request.Audience}");
        if (!string.IsNullOrWhiteSpace(request.Features)) sb.AppendLine($"Key features: {request.Features}");
        sb.AppendLine();
        sb.AppendLine("Technology stack (use exactly these -- do not substitute):");
        sb.AppendLine($"- Backend: {stack.Backend}");
        sb.AppendLine($"- UI framework: {stack.Ui}");
        sb.AppendLine($"- Database: {stack.Database}");
        sb.AppendLine($"- Containerization: {stack.Infra}");
        sb.AppendLine($"- UI style: {stack.UiStyle}");
    }

    private static string BuildPhasePrompt(
        StartBuildRequest request,
        BuildPhase phase,
        IReadOnlyList<SpecDocumentDto> sharedContext,
        IReadOnlyList<GeneratedFile> existingFiles)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are building one project incrementally, in phases. This is phase {phase.Index}: {phase.Name}.");
        sb.AppendLine();
        AppendProjectBrief(sb, request);
        sb.AppendLine();
        sb.AppendLine("=== THIS PHASE ===");
        sb.AppendLine(phase.Goal);
        sb.AppendLine();
        sb.AppendLine(
            "Return the files this phase is responsible for. Return each file complete and compilable -- "
            + "never a diff, never a placeholder. You may also return a file an earlier phase wrote if this "
            + "phase genuinely needs to extend it (for example registering new services in Program.cs); "
            + "return it complete, with the earlier content preserved.");

        if (existingFiles.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"=== FILES EARLIER PHASES ALREADY WROTE ({existingFiles.Count}) ===");
            sb.AppendLine("Build on these. Do not duplicate or contradict them; do not re-emit one unless you are changing it.");
            foreach (var file in existingFiles)
            {
                sb.AppendLine($"- {file.Path}");
            }

            // Later phases routinely have to extend the wiring files -- register
            // a service in Program.cs, add a package to the csproj, add a
            // dependency to package.json. Returning one of those means
            // returning it whole, which is impossible from a path alone: the
            // model would have to invent the earlier content and would silently
            // destroy it. These few files are therefore sent in full.
            var wiringFiles = SelectWiringFiles(existingFiles);
            if (wiringFiles.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== CURRENT CONTENT OF THE SHARED WIRING FILES ===");
                sb.AppendLine("If this phase needs to extend one of these, return it complete, preserving everything below.");
                foreach (var file in wiringFiles)
                {
                    sb.AppendLine($"--- {file.Path} ---");
                    sb.AppendLine(Truncate(file.Content, 6000));
                    sb.AppendLine();
                }
            }
        }

        if (sharedContext.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== PRODUCT CONTEXT (background -- implement only the specifications below it) ===");
            foreach (var document in sharedContext)
            {
                sb.AppendLine($"--- {document.Path} ---");
                sb.AppendLine(Truncate(document.Content, 8000));
                sb.AppendLine();
            }
        }

        sb.AppendLine($"=== SPECIFICATIONS TO IMPLEMENT IN THIS PHASE ({phase.Specs.Count}) ===");
        sb.AppendLine("Every one of these must be implemented in the files you return. Implement them fully, not as stubs.");
        sb.AppendLine();
        foreach (var document in phase.Specs)
        {
            AppendSpecification(sb, document);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Asks for the specifications the coverage check could find no
    /// implementation of. They are sent in full: the model has no memory of
    /// the phase that should have covered them, and a summary would invite the
    /// same omission a second time.
    /// </summary>
    private static string BuildGapFillingPrompt(
        StartBuildRequest request,
        IReadOnlyList<SpecDocumentDto> missingSpecs,
        IReadOnlyList<GeneratedFile> existingFiles)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"The project for \"{request.SpecificationName}\" has been generated, but {missingSpecs.Count} "
            + "specification(s) below have no implementation in it yet.");
        sb.AppendLine();
        AppendProjectBrief(sb, request);
        sb.AppendLine();
        sb.AppendLine(
            "Implement the missing specifications now. Return the new files, plus any existing file you must "
            + "change to wire them in (complete, with existing content preserved). Do not restate work that is "
            + "already present, and do not return unchanged files.");

        sb.AppendLine();
        sb.AppendLine($"=== FILES ALREADY IN THE PROJECT ({existingFiles.Count}) ===");
        foreach (var file in existingFiles)
        {
            sb.AppendLine($"- {file.Path}");
        }

        var wiringFiles = SelectWiringFiles(existingFiles);
        if (wiringFiles.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== CURRENT CONTENT OF THE SHARED WIRING FILES ===");
            sb.AppendLine("If wiring the missing work in means changing one of these, return it complete, preserving everything below.");
            foreach (var file in wiringFiles)
            {
                sb.AppendLine($"--- {file.Path} ---");
                sb.AppendLine(Truncate(file.Content, 6000));
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine($"=== SPECIFICATIONS STILL TO IMPLEMENT ({missingSpecs.Count}) ===");
        foreach (var document in missingSpecs)
        {
            AppendSpecification(sb, document);
        }

        return sb.ToString();
    }

    /// <summary>
    /// The files every phase may need to extend: the composition root, the
    /// project manifests and the compose file. Deliberately a short, fixed
    /// list -- sending the whole project would grow each prompt with the
    /// accumulated repository, which is exactly what phasing exists to avoid.
    /// </summary>
    private static IReadOnlyList<GeneratedFile> SelectWiringFiles(IReadOnlyList<GeneratedFile> files)
    {
        var wanted = new[]
        {
            $"{ProjectLayout.BackendDirectory}/Program.cs",
            $"{ProjectLayout.BackendDirectory}/appsettings.json",
            $"{ProjectLayout.FrontendDirectory}/package.json",
            "docker-compose.yml",
            "Directory.Packages.props",
        };

        var selected = files
            .Where(f => wanted.Contains(f.Path, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // The backend host project file, whose name the model chose.
        var hostProject = files.FirstOrDefault(f =>
            f.Path.StartsWith($"{ProjectLayout.BackendDirectory}/", StringComparison.OrdinalIgnoreCase)
            && f.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            && !f.Path[(ProjectLayout.BackendDirectory.Length + 1)..].Contains('/'));

        if (hostProject is not null) selected.Add(hostProject);

        return selected;
    }

    /// <summary>
    /// Writes one specification into a prompt with its identity intact -- the
    /// id and declared outputs are what the coverage check later looks for, so
    /// the model is told the terms it will be measured against.
    /// </summary>
    private static void AppendSpecification(StringBuilder sb, SpecDocumentDto document)
    {
        var heading = string.IsNullOrWhiteSpace(document.SpecId)
            ? $"--- {document.Path} ---"
            : $"--- {document.SpecId}: {document.Title} ({document.Path}) ---";

        sb.AppendLine(heading);
        if (document.Generates is { Count: > 0 })
        {
            sb.AppendLine($"(this specification is expected to produce: {string.Join(", ", document.Generates)})");
        }
        sb.AppendLine(document.Content);
        sb.AppendLine();
    }

    private static string BuildRepairPrompt(
        StartBuildRequest request, IReadOnlyList<GeneratedFile> currentFiles, string stepName, string errorLog,
        bool escalate)
    {
        // The diagnostics name the files actually at fault; sending only those
        // (plus the manifests every step depends on) keeps the prompt's size
        // bounded by the failure instead of by the size of the whole project,
        // and stops the model from being tempted to rewrite files that were
        // never implicated. Falls back to the full file set when the log
        // doesn't name anything recognizable (e.g. a Docker/integration
        // failure), since narrowing on nothing would just hide the project.
        var relevantFiles = escalate
            ? []
            : DiagnosticFileExtractor.ExtractRelevantFiles(errorLog, currentFiles);
        var filesToSend = relevantFiles.Count > 0
            ? MergeWithProjectManifests(relevantFiles, currentFiles)
            : currentFiles;

        var sb = new StringBuilder();
        sb.AppendLine($"The generated project for \"{request.SpecificationName}\" failed the \"{stepName}\" step.");
        sb.AppendLine("Fix the underlying cause and return ONLY the files that need to change or be added.");
        sb.AppendLine("Do not return unchanged files. Return each changed file complete -- never a partial diff.");
        if (escalate)
        {
            sb.AppendLine();
            sb.AppendLine(
                "Previous attempts at this fix did not resolve it -- the same failure came back. Do not repeat "
                + "the same change. The whole repository is included below, so look beyond the files the error "
                + "names: the cause is likely somewhere the log does not point at.");
        }
        sb.AppendLine();
        sb.AppendLine("=== FAILURE OUTPUT ===");
        sb.AppendLine(Truncate(errorLog, 12000));
        sb.AppendLine();
        if (filesToSend.Count < currentFiles.Count)
        {
            sb.AppendLine($"=== FILES NAMED IN THE FAILURE OUTPUT ({filesToSend.Count} of {currentFiles.Count} in the project) ===");
            sb.AppendLine("Only the files below are included, because the failure output identifies them specifically.");
            sb.AppendLine("If fixing this genuinely requires touching a file not shown here, you may still return it by path.");
        }
        else
        {
            sb.AppendLine("=== CURRENT REPOSITORY ===");
        }
        foreach (var file in filesToSend)
        {
            sb.AppendLine($"--- {file.Path} ---");
            sb.AppendLine(Truncate(file.Content, 6000));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Always keeps the project manifests (.csproj, package.json, tsconfig,
    /// docker-compose.yml) in the narrowed set even when the diagnostics don't
    /// name them directly -- a missing/incompatible reference is often only
    /// visible from the manifest, not the file that failed to compile.
    /// </summary>
    private static IReadOnlyList<GeneratedFile> MergeWithProjectManifests(
        IReadOnlyList<GeneratedFile> relevantFiles, IReadOnlyList<GeneratedFile> currentFiles)
    {
        var manifestSuffixes = new[] { ".csproj", "package.json", "tsconfig.json", "docker-compose.yml" };
        var merged = new List<GeneratedFile>(relevantFiles);
        var included = relevantFiles.Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in currentFiles)
        {
            if (included.Contains(file.Path)) continue;
            if (manifestSuffixes.Any(suffix => file.Path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            {
                merged.Add(file);
                included.Add(file.Path);
            }
        }
        return merged;
    }

    /// <summary>
    /// Pulls the file list out of the reply. Models routinely wrap JSON in
    /// markdown fences or add a sentence of preamble despite instructions, so
    /// this slices from the first '{' to the last '}' rather than trusting the
    /// whole response to be valid JSON.
    /// </summary>
    private static IReadOnlyList<GeneratedFile> ParseFiles(string responseText)
    {
        var start = responseText.IndexOf('{');
        var end = responseText.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("The model's reply contained no JSON object of generated files.");
        }

        var json = responseText[start..(end + 1)];
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"The model's reply was not valid JSON: {ex.Message}", ex);
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("files", out var filesElement) || filesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("The model's reply had no \"files\" array.");
            }

            var files = filesElement.EnumerateArray()
                .Select(element => new GeneratedFile(
                    element.TryGetProperty("path", out var path) ? path.GetString() ?? "" : "",
                    element.TryGetProperty("content", out var content) ? content.GetString() ?? "" : ""))
                .Where(file => IsSafeRelativePath(file.Path))
                .ToList();

            if (files.Count == 0)
            {
                throw new InvalidOperationException("The model returned an empty file list.");
            }
            return files;
        }
    }

    /// <summary>
    /// Guards the workspace against path traversal: the model's output is
    /// untrusted input that gets written straight to disk, so "../" and
    /// absolute paths must never make it out of the build directory.
    /// </summary>
    private static bool IsSafeRelativePath(string path) =>
        !string.IsNullOrWhiteSpace(path)
        && !System.IO.Path.IsPathRooted(path)
        && !path.Contains("..", StringComparison.Ordinal)
        && !path.StartsWith('/')
        && !path.Contains(':', StringComparison.Ordinal);

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + $"\n… (truncated, {value.Length - max} more characters)";
}
