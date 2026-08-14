using System.Text;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

/// <summary>
/// Renders a parsed batch file's structured front-matter fields into an
/// actual YAML block prepended to its content -- the Api renders this
/// itself rather than trusting the model to hand-write valid YAML syntax,
/// which also makes duplicate-ID/dangling-dependency validation a plain
/// query over structured columns instead of a markdown/YAML parse.
/// </summary>
public static class SpecificationDocumentRenderer
{
    public static SpecificationDocument Render(Guid specificationId, Guid? batchId, ParsedBatchFile file)
    {
        var frontMatter = RenderFrontMatter(file);
        var content = $"{frontMatter}\n\n{file.Content.TrimStart('\n')}".TrimEnd() + "\n";

        return new SpecificationDocument
        {
            Id = Guid.NewGuid(),
            SpecificationId = specificationId,
            BatchId = batchId,
            Path = file.Path,
            SpecId = file.SpecId,
            Title = file.Title,
            Component = file.Component,
            Status = file.Status,
            Version = file.Version,
            DependsOn = file.DependsOn.ToList(),
            Provides = file.Provides.ToList(),
            Generates = file.Generates.ToList(),
            Content = content,
        };
    }

    private static string RenderFrontMatter(ParsedBatchFile file)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.Append("id: ").AppendLine(file.SpecId);
        sb.Append("title: ").AppendLine(file.Title);
        sb.Append("component: ").AppendLine(file.Component);
        sb.Append("status: ").AppendLine(file.Status);
        sb.Append("version: ").AppendLine(file.Version);
        sb.Append("depends_on: ").AppendLine(RenderList(file.DependsOn));
        if (file.Provides.Count > 0)
        {
            sb.Append("provides: ").AppendLine(RenderList(file.Provides));
        }
        if (file.Generates.Count > 0)
        {
            sb.AppendLine("generates:");
            foreach (var path in file.Generates)
            {
                sb.Append("  - ").AppendLine(path);
            }
        }
        sb.Append("---");
        return sb.ToString();
    }

    private static string RenderList(IReadOnlyList<string> values) =>
        values.Count == 0 ? "[]" : $"[{string.Join(", ", values)}]";
}
