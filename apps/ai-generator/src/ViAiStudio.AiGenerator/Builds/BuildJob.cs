namespace ViAiStudio.AiGenerator.Builds;

public enum BuildJobStatus { Running, Ready, Failed }

public sealed class BuildJob
{
    public required string JobId { get; init; }
    public required Guid GenerationId { get; init; }
    public BuildJobStatus Status { get; set; } = BuildJobStatus.Running;
    public int ProgressPct { get; set; }
    public string CurrentStep { get; set; } = "Queued";
}
