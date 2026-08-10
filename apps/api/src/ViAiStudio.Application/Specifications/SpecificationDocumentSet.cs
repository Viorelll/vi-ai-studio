using System.Text;
using System.Text.RegularExpressions;
using ViAiStudio.Domain.Catalog;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

/// <summary>
/// The individual markdown files a finalized specification's download bundle
/// is made of: one overview file, then one file per phase -- further split
/// into one file per checklist item when <see cref="GeneratePhaseTextCommand"/>'s
/// prompt got the model to structure its output with "## &lt;item&gt;" headings
/// (it's asked to whenever a phase has more than one item; see its BuildPrompt).
/// Falls back to a single file per phase when no matching headings are found,
/// so specs generated before this existed (or by a model that ignored the
/// instruction) still export cleanly instead of losing content.
/// </summary>
public static partial class SpecificationDocumentSet
{
    public sealed record Document(string Path, string Content);

    public static IReadOnlyList<Document> Build(Specification specification)
    {
        var documents = new List<Document> { new("00-overview.md", RenderOverview(specification)) };

        foreach (var phase in specification.Phases.OrderBy(p => p.PhaseIndex))
        {
            if (!SpecificationPhaseCatalog.Exists(phase.PhaseIndex)) continue;
            documents.AddRange(BuildPhaseDocuments(phase, SpecificationPhaseCatalog.ByIndex(phase.PhaseIndex)));
        }

        return documents;
    }

    private static string RenderOverview(Specification specification)
    {
        var sb = new StringBuilder();
        sb.Append("# ").AppendLine(specification.Name);
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(specification.Summary))
        {
            sb.Append('_').Append(specification.Summary).AppendLine("_");
            sb.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(specification.Audience))
        {
            sb.Append("**Target audience:** ").AppendLine(specification.Audience);
            sb.AppendLine();
        }
        var stack = specification.Stack;
        sb.Append("**Stack:** ")
            .AppendLine($"{stack.Backend}, {stack.Ui}, {stack.Database}, {stack.Infra}, {stack.UiStyle}");
        return sb.ToString();
    }

    private static IEnumerable<Document> BuildPhaseDocuments(SpecificationPhase phase, SpecificationPhaseDefinition definition)
    {
        var folder = $"{definition.Index + 1:D2}-{Slugify(definition.Title)}";
        var sections = SplitByItemHeadings(phase.GeneratedText, definition.Items);

        if (sections.Count == 0)
        {
            yield return new Document($"{folder}.md", RenderPhaseFile(phase, definition));
            yield break;
        }

        foreach (var (item, content) in sections)
        {
            yield return new Document($"{folder}/{Slugify(item)}.md", RenderItemFile(definition, item, content));
        }
    }

    private static string RenderPhaseFile(SpecificationPhase phase, SpecificationPhaseDefinition definition)
    {
        var sb = new StringBuilder();
        sb.Append("# ").AppendLine(definition.Title);
        sb.AppendLine();
        sb.Append("_Produces: ").Append(definition.Output).AppendLine("_");
        sb.AppendLine();
        foreach (var item in definition.Items)
        {
            var mark = phase.CheckedItems.Contains(item) ? "x" : " ";
            sb.Append("- [").Append(mark).Append("] ").AppendLine(item);
        }
        if (!string.IsNullOrWhiteSpace(phase.GeneratedText))
        {
            sb.AppendLine().AppendLine(phase.GeneratedText.Trim());
        }
        else if (phase.SelectedKeywords.Count > 0)
        {
            sb.AppendLine().Append("_Keywords: ").Append(string.Join(", ", phase.SelectedKeywords)).AppendLine("_");
        }
        return sb.ToString();
    }

    private static string RenderItemFile(SpecificationPhaseDefinition definition, string item, string content)
    {
        var sb = new StringBuilder();
        sb.Append("# ").AppendLine(item);
        sb.AppendLine();
        sb.Append("_Phase ").Append(definition.Index + 1).Append(" — ").Append(definition.Title).AppendLine("_");
        sb.AppendLine();
        sb.AppendLine(content);
        return sb.ToString();
    }

    /// <summary>
    /// Looks for "## &lt;item&gt;" (any heading level) matching one of
    /// <paramref name="items"/> and slices the text between consecutive matches.
    /// Returns an empty list -- signaling the caller to fall back to one file
    /// for the whole phase -- when there's only one item, no text yet, or no
    /// heading in the text matches a known item.
    /// </summary>
    private static IReadOnlyList<(string Item, string Content)> SplitByItemHeadings(string? generatedText, IReadOnlyList<string> items)
    {
        if (string.IsNullOrWhiteSpace(generatedText) || items.Count < 2)
        {
            return [];
        }

        var matches = HeadingRegex().Matches(generatedText)
            .Select(m => (Match: m, Item: items.FirstOrDefault(i => string.Equals(i, m.Groups["title"].Value.Trim(), StringComparison.OrdinalIgnoreCase))))
            .Where(x => x.Item is not null)
            .ToList();

        if (matches.Count == 0)
        {
            return [];
        }

        var sections = new List<(string, string)>();
        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Match.Index + matches[i].Match.Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Match.Index : generatedText.Length;
            var content = generatedText[start..end].Trim();
            if (content.Length > 0)
            {
                sections.Add((matches[i].Item!, content));
            }
        }
        return sections;
    }

    private static string Slugify(string value) => SlugRegex().Replace(value.ToLowerInvariant(), "-").Trim('-');

    [GeneratedRegex(@"^#{1,6}\s+(?<title>.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
