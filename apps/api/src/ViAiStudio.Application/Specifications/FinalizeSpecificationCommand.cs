using System.Text;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Catalog;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record FinalizeSpecificationCommand(Guid SpecificationId);

/// <summary>
/// Renders the full specification markdown from all 15 phases once the
/// wizard is complete and moves the specification from Draft to Ready.
/// This is the only transition of the specification's own status -- AI
/// Build (see StartBuildCommand) is a separate, explicit step whose
/// individual run outcomes never feed back into it.
/// </summary>
public sealed class FinalizeSpecificationHandler(ISpecificationRepository specificationRepository)
{
    public async Task<Specification> HandleAsync(FinalizeSpecificationCommand command, CancellationToken cancellationToken)
    {
        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        specification.SpecMarkdown = RenderMarkdown(specification);
        specification.Status = SpecificationStatus.Ready;

        var firstPhaseText = specification.Phases.SingleOrDefault(p => p.PhaseIndex == 0)?.GeneratedText;
        if (!string.IsNullOrWhiteSpace(firstPhaseText))
        {
            specification.Description = firstPhaseText;
        }

        var notes = specification.Phases
            .Where(p => !string.IsNullOrWhiteSpace(p.GeneratedText) && SpecificationPhaseCatalog.Exists(p.PhaseIndex))
            .OrderBy(p => p.PhaseIndex)
            .Select(p => $"{SpecificationPhaseCatalog.ByIndex(p.PhaseIndex).Title}: {p.GeneratedText!.Trim()}");
        var joinedNotes = string.Join('\n', notes);
        if (!string.IsNullOrWhiteSpace(joinedNotes))
        {
            specification.Features = joinedNotes;
        }

        await specificationRepository.SaveChangesAsync(cancellationToken);
        return specification;
    }

    private static string RenderMarkdown(Specification specification)
    {
        var sb = new StringBuilder();
        sb.Append("# ").AppendLine(specification.Name);
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(specification.Summary))
        {
            sb.Append('_').Append(specification.Summary).AppendLine("_");
        }
        if (!string.IsNullOrWhiteSpace(specification.Audience))
        {
            sb.AppendLine().Append("**Target audience:** ").AppendLine(specification.Audience);
        }
        sb.AppendLine();

        foreach (var phase in specification.Phases.OrderBy(p => p.PhaseIndex))
        {
            if (!SpecificationPhaseCatalog.Exists(phase.PhaseIndex)) continue;
            var definition = SpecificationPhaseCatalog.ByIndex(phase.PhaseIndex);
            sb.Append("## Phase ").Append(phase.PhaseIndex + 1).Append(" — ").AppendLine(definition.Title);
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
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
