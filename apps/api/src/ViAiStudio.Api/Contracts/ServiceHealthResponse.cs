using ViAiStudio.Application.Common;

namespace ViAiStudio.Api.Contracts;

public sealed record ServiceHealthResponse(string Name, bool Online, string Endpoint, string Description)
{
    public static ServiceHealthResponse FromModel(ServiceHealth health) =>
        new(health.Name, health.Online, health.Endpoint, health.Description);
}
