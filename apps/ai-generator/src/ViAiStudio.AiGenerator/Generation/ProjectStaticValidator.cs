using System.Text.Json;
using System.Xml.Linq;
using ViAiStudio.AiGenerator.Sandbox;

namespace ViAiStudio.AiGenerator.Generation;

/// <summary>
/// Cheap structural checks that run against the in-memory file list before a
/// single Docker container is started. Most first-pass failures aren't
/// domain-logic bugs -- they're a missing package version, a project
/// reference to a file that was never generated, or invalid JSON in
/// package.json -- and a parser can catch those in milliseconds instead of
/// paying for a ~90 second sandbox run to discover the same thing. Findings
/// are returned in the same shape a Docker step would return, so they flow
/// through the existing repair loop unchanged.
/// </summary>
public static class ProjectStaticValidator
{
    public static SandboxRunResult Validate(IReadOnlyList<GeneratedFile> files)
    {
        var problems = new List<string>();
        var byPath = files.ToDictionary(f => NormalizePath(f.Path), f => f, StringComparer.OrdinalIgnoreCase);

        ValidateBackend(files, byPath, problems);
        ValidateFrontend(byPath, problems);
        ValidateRootFiles(byPath, problems);

        return problems.Count == 0
            ? new SandboxRunResult(true, 0, "Static validation passed.")
            : SandboxRunResult.Failed(
                "Static validation failed before any container was started:\n- " + string.Join("\n- ", problems));
    }

    private static void ValidateBackend(
        IReadOnlyList<GeneratedFile> files, IReadOnlyDictionary<string, GeneratedFile> byPath, List<string> problems)
    {
        var csprojFiles = files
            .Where(f => f.Path.StartsWith($"{ProjectLayout.BackendDirectory}/", StringComparison.OrdinalIgnoreCase)
                     && f.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var hostProject = csprojFiles.FirstOrDefault(f =>
            !f.Path[(ProjectLayout.BackendDirectory.Length + 1)..].Contains('/'));

        if (hostProject is null)
        {
            problems.Add(
                $"No .csproj sits directly at `{ProjectLayout.BackendDirectory}/` -- `dotnet build` in that " +
                "directory has nothing to build. The runnable API host project must be at the top level of " +
                $"`{ProjectLayout.BackendDirectory}/`, not nested in a subdirectory.");
        }

        // Central Package Management (Directory.Packages.props) deliberately
        // omits Version from individual PackageReference elements -- don't
        // flag that as a missing pin when the project has opted into it.
        var usesCentralPackageManagement = files.Any(f =>
            System.IO.Path.GetFileName(f.Path).Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase));

        foreach (var csproj in csprojFiles)
        {
            XDocument document;
            try
            {
                document = XDocument.Parse(csproj.Content);
            }
            catch (Exception ex)
            {
                problems.Add($"`{csproj.Path}` is not well-formed XML: {ex.Message}");
                continue;
            }

            var projectDirectory = GetDirectory(csproj.Path);

            foreach (var reference in document.Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(include)) continue;

                var resolved = ResolveRelative(projectDirectory, include);
                if (!byPath.ContainsKey(resolved))
                {
                    problems.Add($"`{csproj.Path}` has a ProjectReference to `{include}`, which was never generated (resolved to `{resolved}`).");
                }
            }

            if (!usesCentralPackageManagement)
            {
                foreach (var package in document.Descendants("PackageReference"))
                {
                    var include = package.Attribute("Include")?.Value;
                    if (string.IsNullOrWhiteSpace(include)) continue;

                    var version = package.Attribute("Version")?.Value ?? package.Element("Version")?.Value;
                    if (string.IsNullOrWhiteSpace(version))
                    {
                        problems.Add($"`{csproj.Path}` references package `{include}` with no pinned version -- restore will fail to resolve it.");
                    }
                }
            }
        }
    }

    private static void ValidateFrontend(IReadOnlyDictionary<string, GeneratedFile> byPath, List<string> problems)
    {
        var packageJsonPath = $"{ProjectLayout.FrontendDirectory}/package.json";
        if (!byPath.TryGetValue(packageJsonPath, out var packageJson))
        {
            problems.Add($"`{packageJsonPath}` does not exist -- `npm install`/`npm run build` has nothing to work with.");
            return;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(packageJson.Content);
        }
        catch (JsonException ex)
        {
            problems.Add($"`{packageJsonPath}` is not valid JSON: {ex.Message}");
            return;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("scripts", out var scripts)
                || scripts.ValueKind != JsonValueKind.Object
                || !scripts.TryGetProperty("build", out _))
            {
                problems.Add($"`{packageJsonPath}` has no `scripts.build` entry -- the pipeline runs `npm run build` and has nothing to invoke.");
            }
        }

        foreach (var (path, file) in byPath)
        {
            if (!path.StartsWith($"{ProjectLayout.FrontendDirectory}/", StringComparison.OrdinalIgnoreCase)) continue;
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                JsonDocument.Parse(file.Content).Dispose();
            }
            catch (JsonException ex)
            {
                problems.Add($"`{file.Path}` is not valid JSON: {ex.Message}");
            }
        }
    }

    private static void ValidateRootFiles(IReadOnlyDictionary<string, GeneratedFile> byPath, List<string> problems)
    {
        if (!byPath.TryGetValue("docker-compose.yml", out var compose) && !byPath.ContainsKey("docker-compose.yaml"))
        {
            problems.Add("No `docker-compose.yml` exists at the repository root.");
        }
        else if (compose is not null && LooksMalformedYaml(compose.Content, out var reason))
        {
            problems.Add($"`docker-compose.yml` {reason}");
        }

        if (!byPath.ContainsKey("readme.md"))
        {
            problems.Add("No `README.md` exists at the repository root.");
        }
    }

    /// <summary>
    /// Not a real YAML parser -- just enough to catch the two mistakes models
    /// actually make: emitting an empty file, or indenting with tabs (which
    /// every YAML parser rejects outright).
    /// </summary>
    private static bool LooksMalformedYaml(string content, out string reason)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            reason = "is empty.";
            return true;
        }

        if (content.Contains('\t'))
        {
            reason = "uses tab characters for indentation, which is invalid YAML.";
            return true;
        }

        if (!content.Contains("services:", StringComparison.Ordinal))
        {
            reason = "has no top-level `services:` key.";
            return true;
        }

        reason = "";
        return false;
    }

    private static string GetDirectory(string path)
    {
        var normalized = NormalizePath(path);
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash < 0 ? "" : normalized[..lastSlash];
    }

    private static string ResolveRelative(string fromDirectory, string relativePath)
    {
        var combined = string.IsNullOrEmpty(fromDirectory)
            ? relativePath
            : $"{fromDirectory}/{relativePath}";

        var segments = NormalizePath(combined).Split('/');
        var stack = new List<string>();
        foreach (var segment in segments)
        {
            if (segment is "" or ".") continue;
            if (segment == "..")
            {
                if (stack.Count > 0) stack.RemoveAt(stack.Count - 1);
                continue;
            }
            stack.Add(segment);
        }
        return string.Join('/', stack);
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');
}
