using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

/// <summary>
/// Seeds the reusable authoring content (chip groups, interview rounds,
/// authoring rules, the ID scheme, templates, batch instructions) the
/// specification wizard is built from into <see cref="SpecificationPromptTemplate"/>
/// rows, from the embedded files under PromptLibrarySeedData/ -- kept as plain
/// .md/.json files (diffable against specification-example/) rather than as
/// C# string literals. Upserts by <see cref="SpecificationPromptTemplate.Key"/>:
/// inserts what's missing, and bumps <see cref="SpecificationPromptTemplate.Version"/>
/// on an existing row only when its content actually changed, so prompt wording
/// can keep being edited here pre-production without a data migration.
/// </summary>
public static class SpecificationPromptLibrarySeeder
{
    private const string ResourcePrefix = "ViAiStudio.Infrastructure.Persistence.PromptLibrarySeedData.";

    public static async Task SeedAsync(ViAiStudioDbContext db, CancellationToken cancellationToken = default)
    {
        var assembly = typeof(SpecificationPromptLibrarySeeder).Assembly;
        var existing = await db.SpecificationPromptTemplates.ToDictionaryAsync(t => t.Key, cancellationToken);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var (key, content) = await ReadResourceAsync(assembly, resourceName, cancellationToken);
            var (stage, category, title, orderIndex) = Classify(key, content);

            if (existing.TryGetValue(key, out var template))
            {
                if (!string.Equals(template.Content, content, StringComparison.Ordinal))
                {
                    template.Content = content;
                    template.Version += 1;
                }
                template.Stage = stage;
                template.Category = category;
                template.Title = title;
                template.OrderIndex = orderIndex;
            }
            else
            {
                db.SpecificationPromptTemplates.Add(new SpecificationPromptTemplate
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    Stage = stage,
                    Category = category,
                    Title = title,
                    Content = content,
                    OrderIndex = orderIndex,
                    Version = 1,
                    IsActive = true,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<(string Key, string Content)> ReadResourceAsync(
        Assembly assembly, string resourceName, CancellationToken cancellationToken)
    {
        var suffix = resourceName[ResourcePrefix.Length..];
        var key = suffix.EndsWith(".json", StringComparison.Ordinal) ? suffix[..^5]
            : suffix.EndsWith(".md", StringComparison.Ordinal) ? suffix[..^3]
            : throw new InvalidOperationException($"Unrecognized prompt library seed file extension: '{resourceName}'.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        var content = (await reader.ReadToEndAsync(cancellationToken)).Trim();
        return (key, content);
    }

    /// <summary>
    /// Derives stage/category/title/order from the key's naming convention
    /// instead of a separate metadata table, so adding a seed file only means
    /// adding the file.
    /// </summary>
    private static (SpecificationPromptStage Stage, string Category, string Title, int OrderIndex) Classify(string key, string content)
    {
        if (key.StartsWith("chips.group.", StringComparison.Ordinal))
        {
            var letter = key["chips.group.".Length..];
            return (SpecificationPromptStage.ChipSelection, "chip-group",
                ExtractJsonString(content, "label") ?? $"Group {letter.ToUpperInvariant()}", letter[0] - 'a');
        }

        if (key.StartsWith("interview.round.", StringComparison.Ordinal))
        {
            var round = int.Parse(key["interview.round.".Length..]);
            return (SpecificationPromptStage.DomainInterview, "interview-round",
                ExtractJsonString(content, "title") ?? $"Round {round}", round);
        }

        if (key.StartsWith("generation.batch.", StringComparison.Ordinal))
        {
            var batch = int.Parse(key["generation.batch.".Length..]);
            return (SpecificationPromptStage.Generation, "batch-instructions", $"Batch {batch}", batch);
        }

        if (key.StartsWith("generation.template.", StringComparison.Ordinal))
        {
            var name = key["generation.template.".Length..];
            return (SpecificationPromptStage.Generation, "template", $"{Capitalize(name)} template", 0);
        }

        return key switch
        {
            "interview.completeness" => (SpecificationPromptStage.DomainInterview, "interview-completeness", "Completeness checklist", 8),
            "interview.expand-helper.system" => (SpecificationPromptStage.DomainInterview, "system-prompt", "Interview answer helper", 0),
            "generation.authoring-rules" => (SpecificationPromptStage.Generation, "authoring-rule", "Authoring rules", 0),
            "generation.id-scheme" => (SpecificationPromptStage.Generation, "id-scheme", "ID scheme", 0),
            "generation.output-shape" => (SpecificationPromptStage.Generation, "authoring-rule", "Output shape", 1),
            "generation.file-rules" => (SpecificationPromptStage.Generation, "authoring-rule", "File rules", 2),
            "generation.consistency-rules" => (SpecificationPromptStage.Generation, "authoring-rule", "Consistency rules", 3),
            "generation.response-format" => (SpecificationPromptStage.Generation, "response-format", "Response format", 0),
            "generation.system-prompt" => (SpecificationPromptStage.Generation, "system-prompt", "Batch system prompt", 0),
            _ => throw new InvalidOperationException($"Unrecognized prompt library seed key '{key}'."),
        };
    }

    private static string? ExtractJsonString(string json, string property)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty(property, out var value) ? value.GetString() : null;
    }

    private static string Capitalize(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
