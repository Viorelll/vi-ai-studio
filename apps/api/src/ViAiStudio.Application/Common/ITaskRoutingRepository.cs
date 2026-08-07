using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface ITaskRoutingRepository
{
    /// <summary>All five task types, seeded unassigned on first access.</summary>
    Task<IReadOnlyList<TaskRouting>> ListAsync(CancellationToken cancellationToken);

    Task<TaskRouting?> GetAsync(AiTaskType task, CancellationToken cancellationToken);
    Task UpsertAsync(TaskRouting routing, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
