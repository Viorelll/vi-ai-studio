namespace ViAiStudio.Domain.Entities;

/// <summary>
/// A record of one AI Generator request/response pair, kept for the Admin →
/// Audit views. Logged against the specification always, and against a
/// specific <see cref="Generation.Version"/> when the call happened during
/// an AI Build run rather than in the wizard.
/// </summary>
public sealed class AiCallLog
{
    public Guid Id { get; init; }
    public required Guid SpecificationId { get; init; }
    public int? GenerationVersion { get; init; }
    public required AiTaskType Task { get; init; }
    public required string Model { get; init; }
    public int Requests { get; init; } = 1;
    public required int TokensIn { get; init; }
    public required int TokensOut { get; init; }
    public required string Prompt { get; init; }
    public required string Result { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
