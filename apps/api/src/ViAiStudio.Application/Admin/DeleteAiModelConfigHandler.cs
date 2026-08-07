using ViAiStudio.Application.Common;

namespace ViAiStudio.Application.Admin;

/// <summary>Deletes a model configuration and unassigns it from any task it was routed to.</summary>
public sealed class DeleteAiModelConfigHandler(IAiModelConfigRepository configRepository, ITaskRoutingRepository taskRoutingRepository)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var config = await configRepository.GetAsync(id, cancellationToken);
        if (config is null)
        {
            return false;
        }

        var routings = await taskRoutingRepository.ListAsync(cancellationToken);
        foreach (var routing in routings.Where(r => r.AiModelConfigId == id))
        {
            routing.AiModelConfigId = null;
            await taskRoutingRepository.UpsertAsync(routing, cancellationToken);
        }

        await configRepository.RemoveAsync(config, cancellationToken);
        await configRepository.SaveChangesAsync(cancellationToken);
        await taskRoutingRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
