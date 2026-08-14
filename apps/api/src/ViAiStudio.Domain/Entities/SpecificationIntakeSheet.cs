namespace ViAiStudio.Domain.Entities;

/// <summary>
/// The chip-selection ("shape") decisions captured in stage 1 of the
/// specification wizard -- the machine-readable Intake Sheet described in
/// the authoring pipeline's chip-selection prompt. One row per
/// <see cref="Specification"/>, created the first time chips are saved.
/// </summary>
public sealed class SpecificationIntakeSheet
{
    public Guid Id { get; init; }
    public required Guid SpecificationId { get; init; }

    public string ProductShape { get; set; } = string.Empty;
    public string TenantIsolation { get; set; } = string.Empty;
    public string IdentityModel { get; set; } = string.Empty;
    public string PrimaryDatabase { get; set; } = string.Empty;
    public string Frontend { get; set; } = string.Empty;
    public string Rigour { get; set; } = string.Empty;
    public string SpecScope { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;

    public List<string> Deployables { get; set; } = [];
    public List<string> IdentityFeatures { get; set; } = [];
    public List<string> SupportingInfrastructure { get; set; } = [];
    public List<string> FrontendRequirements { get; set; } = [];
    public List<string> FunctionalAreas { get; set; } = [];
    public List<string> Compliance { get; set; } = [];
    public List<string> Environments { get; set; } = [];

    /// <summary>Things the selections force that weren't picked explicitly, computed by IntakeConflictRules.</summary>
    public List<string> ImpliedDecisions { get; set; } = [];

    /// <summary>Contradictions found between selections and how they were settled, computed by IntakeConflictRules.</summary>
    public List<string> ConflictsResolved { get; set; } = [];

    /// <summary>What a specification cannot be written without -- carried into the interview stage as things to resolve.</summary>
    public List<string> StillUnknown { get; set; } = [];

    /// <summary>Set when stage 1 (chip selection) has been saved at least once.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Set when stage 2 (domain interview) has been explicitly completed -- gates whether stage 3 (generation) can start.</summary>
    public DateTimeOffset? InterviewCompletedAt { get; set; }
}
