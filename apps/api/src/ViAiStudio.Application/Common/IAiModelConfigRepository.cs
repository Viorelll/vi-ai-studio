using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface IAiModelConfigRepository
{
    Task AddAsync(AiModelConfig config, CancellationToken cancellationToken);
    Task RemoveAsync(AiModelConfig config, CancellationToken cancellationToken);
    Task<AiModelConfig?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiModelConfig>> ListAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
