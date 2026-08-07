namespace ViAiStudio.Application.Common;

public sealed record ServiceHealth(string Name, bool Online, string Endpoint, string Description);

/// <summary>Live status for the Admin dashboard's service badges (API, AI Generator, Storage).</summary>
public interface IServiceHealthChecker
{
    Task<IReadOnlyList<ServiceHealth>> CheckAllAsync(CancellationToken cancellationToken);
}
