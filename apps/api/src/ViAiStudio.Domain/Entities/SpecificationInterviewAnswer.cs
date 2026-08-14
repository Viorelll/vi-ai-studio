namespace ViAiStudio.Domain.Entities;

/// <summary>
/// One answered (or defaulted) question from stage 2's domain interview.
/// Question text and default hint are snapshotted from the prompt library at
/// ask-time so a previously-recorded answer stays meaningful even if the
/// library's wording is later edited.
/// </summary>
public sealed class SpecificationInterviewAnswer
{
    public Guid Id { get; init; }
    public required Guid SpecificationId { get; init; }

    /// <summary>1-7, one of the domain-interview rounds.</summary>
    public required int RoundIndex { get; init; }
    public required int QuestionIndex { get; init; }

    public required string QuestionText { get; set; }
    public required string DefaultHint { get; set; }
    public string? AnswerText { get; set; }
    public bool UsedDefault { get; set; }
    public DateTimeOffset? AiExpandedAt { get; set; }
}
