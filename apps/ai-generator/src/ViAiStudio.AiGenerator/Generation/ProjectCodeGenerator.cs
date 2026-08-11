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
        You are a senior engineer who generates complete, immediately runnable software projects.
        You always reply with a single JSON object and nothing else -- no prose, no markdown fences.
        The JSON shape is exactly: {"files":[{"path":"relative/path.ext","content":"full file contents"}]}
        Paths are repository-relative and use forward slashes.

        {{ProjectLayout.Contract}}
        """;

    public async Task<CodeGenerationResult> GenerateAsync(StartBuildRequest request, CancellationToken cancellationToken)
    {
        var prompt = BuildGenerationPrompt(request);
        return await CallAsync(request, prompt, cancellationToken);
    }

    /// <summary>
    /// Asks for a fix for one failed verification step. Only the files the model
    /// wants to change come back; the caller overlays them onto the project, so
    /// the model never has to re-emit the whole repository to fix one compile error.
    /// </summary>
    public async Task<CodeGenerationResult> RepairAsync(
        StartBuildRequest request,
        IReadOnlyList<GeneratedFile> currentFiles,
        string stepName,
        string errorLog,
        CancellationToken cancellationToken)
    {
        var prompt = BuildRepairPrompt(request, currentFiles, stepName, errorLog);
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

    private static string BuildGenerationPrompt(StartBuildRequest request)
    {
        var stack = request.Stack;
        var sb = new StringBuilder();
        sb.AppendLine("Generate a complete, working project for the following specification.");
        sb.AppendLine();
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
        sb.AppendLine();
        sb.AppendLine("=== SPECIFICATION ===");
        sb.AppendLine(request.SpecMarkdown);

        foreach (var document in request.Documents)
        {
            sb.AppendLine();
            sb.AppendLine($"--- {document.Path} ---");
            sb.AppendLine(document.Content);
        }

        return sb.ToString();
    }

    private static string BuildRepairPrompt(
        StartBuildRequest request, IReadOnlyList<GeneratedFile> currentFiles, string stepName, string errorLog)
    {
        // The diagnostics name the files actually at fault; sending only those
        // (plus the manifests every step depends on) keeps the prompt's size
        // bounded by the failure instead of by the size of the whole project,
        // and stops the model from being tempted to rewrite files that were
        // never implicated. Falls back to the full file set when the log
        // doesn't name anything recognizable (e.g. a Docker/integration
        // failure), since narrowing on nothing would just hide the project.
        var relevantFiles = DiagnosticFileExtractor.ExtractRelevantFiles(errorLog, currentFiles);
        var filesToSend = relevantFiles.Count > 0
            ? MergeWithProjectManifests(relevantFiles, currentFiles)
            : currentFiles;

        var sb = new StringBuilder();
        sb.AppendLine($"The generated project for \"{request.SpecificationName}\" failed the \"{stepName}\" step.");
        sb.AppendLine("Fix the underlying cause and return ONLY the files that need to change or be added.");
        sb.AppendLine("Do not return unchanged files. Return each changed file complete -- never a partial diff.");
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
