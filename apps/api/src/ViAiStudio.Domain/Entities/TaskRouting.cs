namespace ViAiStudio.Domain.Entities;

public enum AiTaskType
{
    /// <summary>Drafts markdown for a wizard phase in AI Specification Studio.</summary>
    SpecGeneration,

    /// <summary>Powers AI Build code generation.</summary>
    CodeGeneration,

    /// <summary>Generates product & UI imagery.</summary>
    ImageGeneration,

    /// <summary>Generates audio & music assets.</summary>
    SoundGeneration,

    /// <summary>Speech-to-text for voice input.</summary>
    Transcribing,
}

/// <summary>
/// Which <see cref="AiModelConfig"/> handles a given <see cref="AiTaskType"/>.
/// One row per task type; <see cref="AiModelConfigId"/> is null when the task
/// is unassigned.
/// </summary>
public sealed class TaskRouting
{
    public required AiTaskType Task { get; init; }
    public Guid? AiModelConfigId { get; set; }
}
