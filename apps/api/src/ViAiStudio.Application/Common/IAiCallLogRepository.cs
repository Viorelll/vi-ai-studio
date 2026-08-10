using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

/// <summary>
/// Which half of a specification's AI spend to report on. The two Audit
/// surfaces bill to different activities and must never be added together:
/// authoring a specification in the wizard is separate work from generating
/// project code from it. <see cref="AiCallLog.GenerationVersion"/> is the
/// discriminator -- null for wizard calls, set to the build version otherwise.
/// </summary>
public enum AiCallLogScope
{
    /// <summary>Everything logged against the specification, both scopes combined.</summary>
    All,

    /// <summary>Specification authoring only (wizard chips and phase drafting).</summary>
    Specification,

    /// <summary>AI Build runs only, across every generated version.</summary>
    Build,
}

public interface IAiCallLogRepository
{
    Task AddAsync(AiCallLog log, CancellationToken cancellationToken);
    Task<AiCallLog?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AiCallLog>> ListBySpecificationAsync(
        Guid specificationId, AiCallLogScope scope, CancellationToken cancellationToken);

    /// <summary>Per-specification rollups (log count, total requests, total tokens) for the Audit list.</summary>
    Task<IReadOnlyList<AiCallLogRollup>> RollupBySpecificationAsync(
        AiCallLogScope scope, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record AiCallLogRollup(Guid SpecificationId, int LogCount, int TotalRequests, long TotalTokens);
