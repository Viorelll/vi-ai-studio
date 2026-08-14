using System.Text;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

/// <summary>
/// Deterministically renders manifest.md from every document's front-matter
/// -- the model is never asked to write it, since only the Api has a
/// trustworthy global view across all ten batches' allocated IDs. Ports the
/// grouping/table logic of specification-example's generate-manifest.py.
/// </summary>
public static class SpecificationManifestRenderer
{
    public static string Render(Specification specification, IReadOnlyList<SpecificationDocumentSummary> documents)
    {
        var sb = new StringBuilder();
        sb.Append("# ").Append(specification.Name).AppendLine(" -- specification manifest");
        sb.AppendLine();
        sb.Append(documents.Count).AppendLine(" specification documents.");
        sb.AppendLine();
        sb.AppendLine("| ID | Title | Status | Depends on | File |");
        sb.AppendLine("|---|---|---|---|---|");

        foreach (var doc in documents.Where(d => !string.IsNullOrWhiteSpace(d.SpecId)).OrderBy(d => d.SpecId, StringComparer.Ordinal))
        {
            var deps = doc.DependsOn.Count == 0 ? "--" : string.Join(", ", doc.DependsOn.Select(id => $"`{id}`"));
            sb.Append("| `").Append(doc.SpecId).Append("` | ").Append(doc.Title).Append(" | ").Append(doc.Status)
                .Append(" | ").Append(deps).Append(" | [`").Append(doc.Path).Append("`](").Append(doc.Path).AppendLine(") |");
        }

        return sb.ToString();
    }
}

/// <summary>Renders and persists manifest.md as the final document of a generation run.</summary>
public sealed class RenderSpecificationManifestHandler(ISpecificationDocumentRepository documentRepository)
{
    public async Task HandleAsync(Specification specification, CancellationToken cancellationToken)
    {
        var documents = await documentRepository.ListAsync(specification.Id, cancellationToken);
        var body = "# Specification manifest\n\n" + SpecificationManifestRenderer.Render(specification, documents);

        var file = new ParsedBatchFile(
            Path: "manifest.md", SpecId: "META-005", Title: "Specification manifest", Component: "_root",
            Status: "ready", Version: "1.0", DependsOn: [], Provides: [], Generates: [], Content: body);

        var document = SpecificationDocumentRenderer.Render(specification.Id, null, file);
        await documentRepository.UpsertAsync(document, cancellationToken);
        await documentRepository.SaveChangesAsync(cancellationToken);
    }
}
