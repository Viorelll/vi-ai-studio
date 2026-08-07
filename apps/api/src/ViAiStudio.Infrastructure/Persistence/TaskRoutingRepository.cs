using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class TaskRoutingRepository(ViAiStudioDbContext dbContext) : ITaskRoutingRepository
{
    public async Task<IReadOnlyList<TaskRouting>> ListAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.TaskRoutings.ToListAsync(cancellationToken);
        var existingTasks = existing.Select(r => r.Task).ToHashSet();

        foreach (var task in Enum.GetValues<AiTaskType>().Where(t => !existingTasks.Contains(t)))
        {
            var routing = new TaskRouting { Task = task, AiModelConfigId = null };
            dbContext.TaskRoutings.Add(routing);
            existing.Add(routing);
        }

        if (existing.Count > existingTasks.Count)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing.OrderBy(r => r.Task).ToList();
    }

    public Task<TaskRouting?> GetAsync(AiTaskType task, CancellationToken cancellationToken) =>
        dbContext.TaskRoutings.FirstOrDefaultAsync(r => r.Task == task, cancellationToken);

    public async Task UpsertAsync(TaskRouting routing, CancellationToken cancellationToken)
    {
        var existing = await dbContext.TaskRoutings.FirstOrDefaultAsync(r => r.Task == routing.Task, cancellationToken);
        if (existing is null)
        {
            dbContext.TaskRoutings.Add(routing);
        }
        else
        {
            existing.AiModelConfigId = routing.AiModelConfigId;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
