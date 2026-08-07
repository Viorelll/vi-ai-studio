using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Admin;

public sealed record UpdateTaskRoutingCommand(AiTaskType Task, Guid? AiModelConfigId);

public sealed class UpdateTaskRoutingHandler(ITaskRoutingRepository taskRoutingRepository, IAiModelConfigRepository configRepository)
{
    public async Task<TaskRouting> HandleAsync(UpdateTaskRoutingCommand command, CancellationToken cancellationToken)
    {
        if (command.AiModelConfigId is { } configId
            && await configRepository.GetAsync(configId, cancellationToken) is null)
        {
            throw new InvalidOperationException($"Model configuration '{configId}' does not exist.");
        }

        var routing = new TaskRouting { Task = command.Task, AiModelConfigId = command.AiModelConfigId };
        await taskRoutingRepository.UpsertAsync(routing, cancellationToken);
        await taskRoutingRepository.SaveChangesAsync(cancellationToken);

        return routing;
    }
}
