using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Catalog;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record SaveSpecificationPhaseCommand(
    Guid SpecificationId,
    int PhaseIndex,
    List<string> CheckedItems,
    List<string> SelectedKeywords);

/// <summary>Persists checklist/keyword state for one wizard phase (autosaved as the user works through it).</summary>
public sealed class SaveSpecificationPhaseHandler(ISpecificationRepository specificationRepository)
{
    public async Task<SpecificationPhase> HandleAsync(SaveSpecificationPhaseCommand command, CancellationToken cancellationToken)
    {
        _ = SpecificationPhaseCatalog.ByIndex(command.PhaseIndex); // throws for an out-of-range index

        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        var phase = specification.Phases.SingleOrDefault(p => p.PhaseIndex == command.PhaseIndex)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' has no phase at index {command.PhaseIndex}.");

        phase.CheckedItems = command.CheckedItems;
        phase.SelectedKeywords = command.SelectedKeywords;

        await specificationRepository.SaveChangesAsync(cancellationToken);
        return phase;
    }
}
