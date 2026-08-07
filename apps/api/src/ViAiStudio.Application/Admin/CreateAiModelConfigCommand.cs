using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Admin;

public sealed record CreateAiModelConfigCommand(string Label, AiProvider Provider, string Model, string BaseUrl, string ApiKey);

public sealed class CreateAiModelConfigHandler(IAiModelConfigRepository repository)
{
    public async Task<AiModelConfig> HandleAsync(CreateAiModelConfigCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Label) || string.IsNullOrWhiteSpace(command.Model))
        {
            throw new InvalidOperationException("Label and model name are required.");
        }

        var config = new AiModelConfig
        {
            Id = Guid.NewGuid(),
            Label = command.Label,
            Provider = command.Provider,
            Model = command.Model,
            BaseUrl = command.BaseUrl,
            ApiKey = command.ApiKey,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await repository.AddAsync(config, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return config;
    }
}
