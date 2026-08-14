using System.Text.Json;

namespace ViAiStudio.Application.Specifications;

public sealed record ParsedBatchFile(
    string Path, string SpecId, string Title, string Component, string Status, string Version,
    IReadOnlyList<string> DependsOn, IReadOnlyList<string> Provides, IReadOnlyList<string> Generates, string Content);

public sealed record BatchParseResult(IReadOnlyList<ParsedBatchFile> Files, IReadOnlyList<string> Errors);

/// <summary>
/// Pulls the file list out of one batch's reply. Models routinely wrap JSON
/// in markdown fences or add a sentence of preamble despite instructions, so
/// this slices from the first '{' to the last '}' rather than trusting the
/// whole response to be valid JSON -- the same defense AI Generator's
/// ProjectCodeGenerator.ParseFiles already uses for its own JSON file-list
/// protocol when generating project code.
/// </summary>
public static class SpecificationBatchResponseParser
{
    public static BatchParseResult Parse(string responseText)
    {
        var start = responseText.IndexOf('{');
        var end = responseText.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return new BatchParseResult([], ["The model's reply contained no JSON object of files."]);
        }

        var json = responseText[start..(end + 1)];
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return new BatchParseResult([], [$"The model's reply was not valid JSON: {ex.Message}"]);
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("files", out var filesElement) || filesElement.ValueKind != JsonValueKind.Array)
            {
                return new BatchParseResult([], ["The model's reply had no \"files\" array."]);
            }

            var files = new List<ParsedBatchFile>();
            var errors = new List<string>();
            var index = 0;
            foreach (var element in filesElement.EnumerateArray())
            {
                index++;
                var path = GetString(element, "path");
                var content = GetString(element, "content");
                if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(content) || !IsSafeRelativePath(path))
                {
                    errors.Add($"File #{index} in the model's reply was missing a path/content or had an unsafe path -- skipped.");
                    continue;
                }

                files.Add(new ParsedBatchFile(
                    Path: path,
                    SpecId: GetString(element, "specId") ?? "",
                    Title: GetString(element, "title") ?? path,
                    Component: GetString(element, "component") ?? "",
                    Status: GetString(element, "status") == "ready" ? "ready" : "draft",
                    Version: GetString(element, "version") ?? "1.0",
                    DependsOn: GetStringArray(element, "dependsOn"),
                    Provides: GetStringArray(element, "provides"),
                    Generates: GetStringArray(element, "generates"),
                    Content: content));
            }

            if (files.Count == 0)
            {
                errors.Add("The model returned an empty or entirely invalid file list.");
            }

            return new BatchParseResult(files, errors);
        }
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static IReadOnlyList<string> GetStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }
        return value.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .ToList();
    }

    /// <summary>Rejects absolute paths and parent-directory escapes -- mirrors AI Generator's own path-safety check.</summary>
    private static bool IsSafeRelativePath(string path) =>
        !path.StartsWith('/') && !path.Contains("..", StringComparison.Ordinal) && !path.Contains(':') && !path.StartsWith('\\');
}
