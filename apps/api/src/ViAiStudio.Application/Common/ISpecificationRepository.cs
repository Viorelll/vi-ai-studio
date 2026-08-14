using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface ISpecificationRepository
{
    Task AddAsync(Specification specification, CancellationToken cancellationToken);
    Task RemoveAsync(Specification specification, CancellationToken cancellationToken);

    /// <summary>
    /// Explicitly tracks a brand-new child of an already-loaded specification
    /// (a phase, intake sheet, interview answer, document, generation run,
    /// batch, or validation issue) as Added. Simply attaching it via a
    /// navigation property on a specification loaded from a prior query --
    /// e.g. <c>specification.Intake = intake</c> or
    /// <c>specification.InterviewAnswers.Add(answer)</c> -- and relying on
    /// DetectChanges' reachability-based fixup is not reliable for entities
    /// with a client-generated (Guid) key that is already assigned before
    /// the entity becomes reachable: EF Core cannot tell "new, client-keyed
    /// row" apart from "existing row being reattached" from property values
    /// alone, and here it resolves that ambiguity as Modified, which emits
    /// an UPDATE for a row that was never inserted (a
    /// DbUpdateConcurrencyException at 0 rows affected). Only entities added
    /// to a *new, not-yet-tracked* Specification's navigation before its own
    /// <see cref="AddAsync"/> avoid this -- there, the whole graph becomes
    /// Added together by the explicit top-level Add.
    /// </summary>
    Task AddNewChildAsync<TChild>(TChild child, CancellationToken cancellationToken) where TChild : class;

    /// <summary>Loads a specification with its phases and generations.</summary>
    Task<Specification?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Specification>> ListAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
