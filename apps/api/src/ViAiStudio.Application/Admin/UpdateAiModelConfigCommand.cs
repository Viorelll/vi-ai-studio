using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Admin;

public sealed record UpdateAiModelConfigCommand(Guid Id, string Label, AiProvider Provider, string Model, string BaseUrl, string ApiKey);

public sealed class UpdateAiModelConfigHandler(IAiModelConfigRepository repository)
{
    public async Task<AiModelConfig> HandleAsync(UpdateAiModelConfigCommand command, CancellationToken cancellationToken)
    {
        var config = await repository.GetAsync(command.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Model configuration '{command.Id}' does not exist.");

        config.Label = command.Label;
        config.Provider = command.Provider;
        config.Model = command.Model;
        config.BaseUrl = command.BaseUrl;
        config.ApiKey = command.ApiKey;

        await repository.SaveChangesAsync(cancellationToken);
        return config;
    }
}
