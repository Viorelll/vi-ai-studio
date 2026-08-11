using System.Text.RegularExpressions;

namespace ViAiStudio.AiGenerator.Generation;

/// <summary>
/// Pulls file paths out of a build/lint failure log so a repair prompt can
/// send just the files actually implicated instead of the whole repository.
/// dotnet, tsc and eslint all emit diagnostics of the shape
/// "path(line,col): message" or "path:line:col message" -- this matches that
/// shape rather than depending on any tool's structured output format, so it
/// works across all three without needing SARIF or ESLint's --format json.
/// </summary>
public static partial class DiagnosticFileExtractor
{
    [GeneratedRegex(
        @"(?<path>[A-Za-z0-9_.\-\/]+\.(?:cs|csproj|ts|tsx|js|jsx|json|cshtml|razor|yml|yaml))(?:\((?<line1>\d+)[,:]\d+\)|:(?<line2>\d+):\d+)",
        RegexOptions.Compiled)]
    private static partial Regex DiagnosticLocation();

    /// <summary>
    /// Returns the subset of <paramref name="files"/> whose path appears as a
    /// diagnostic location in <paramref name="errorLog"/>. Matching is by file
    /// name suffix against the workspace's own paths, so it doesn't matter
    /// whether the tool printed an absolute container path
    /// (`/workspace/backend/Foo.cs`) or a path relative to its working
    /// directory (`Foo.cs`) -- either way it lands on the same workspace file.
    /// Returns an empty list if nothing in the log matches a known file,
    /// which the caller treats as "couldn't narrow it down".
    /// </summary>
    public static IReadOnlyList<GeneratedFile> ExtractRelevantFiles(string errorLog, IReadOnlyList<GeneratedFile> files)
    {
        var candidatePaths = DiagnosticLocation()
            .Matches(errorLog)
            .Select(m => m.Groups["path"].Value.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (candidatePaths.Count == 0) return [];

        return files
            .Where(f => candidatePaths.Any(candidate =>
                f.Path.Equals(candidate, StringComparison.OrdinalIgnoreCase)
                || f.Path.EndsWith("/" + candidate.TrimStart('/'), StringComparison.OrdinalIgnoreCase)
                || candidate.EndsWith("/" + f.Path, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
