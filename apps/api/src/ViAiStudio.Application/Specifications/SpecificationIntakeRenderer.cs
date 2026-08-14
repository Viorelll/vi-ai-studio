using System.Text;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

/// <summary>
/// Deterministically renders a specification's stage-1/stage-2 answers into
/// the plain-text form fed into every stage-3 batch prompt -- there is no AI
/// round-trip to synthesize "intake/*.md"-equivalent prose first; the
/// structured answers already are the content.
/// </summary>
public static class SpecificationIntakeRenderer
{
    public static string Render(Specification specification)
    {
        var intake = specification.Intake
            ?? throw new InvalidOperationException($"Specification '{specification.Id}' has no intake sheet yet.");

        var sb = new StringBuilder();
        sb.AppendLine("=== INTAKE SHEET (stage 1: chip selection) ===");
        sb.AppendLine($"product_shape: {intake.ProductShape}");
        sb.AppendLine($"tenant_isolation: {intake.TenantIsolation}");
        sb.AppendLine($"deployables: {string.Join(", ", intake.Deployables)}");
        sb.AppendLine($"identity_model: {intake.IdentityModel}");
        sb.AppendLine($"identity_features: {string.Join(", ", intake.IdentityFeatures)}");
        sb.AppendLine($"primary_database: {intake.PrimaryDatabase}");
        sb.AppendLine($"supporting_infrastructure: {string.Join(", ", intake.SupportingInfrastructure)}");
        sb.AppendLine($"frontend: {intake.Frontend}");
        sb.AppendLine($"frontend_requirements: {string.Join(", ", intake.FrontendRequirements)}");
        sb.AppendLine($"functional_areas: {string.Join(", ", intake.FunctionalAreas)}");
        sb.AppendLine($"compliance: {string.Join(", ", intake.Compliance)}");
        sb.AppendLine($"environments: {string.Join(", ", intake.Environments)}");
        sb.AppendLine($"rigour: {intake.Rigour}");
        sb.AppendLine($"spec_scope: {intake.SpecScope}");
        sb.AppendLine($"team: {intake.Team}");
        if (intake.ImpliedDecisions.Count > 0) sb.AppendLine($"implied_decisions: {string.Join("; ", intake.ImpliedDecisions)}");
        if (intake.ConflictsResolved.Count > 0) sb.AppendLine($"conflicts_resolved: {string.Join("; ", intake.ConflictsResolved)}");
        sb.AppendLine();

        sb.AppendLine("=== DOMAIN INTERVIEW (stage 2) ===");
        foreach (var round in specification.InterviewAnswers.GroupBy(a => a.RoundIndex).OrderBy(g => g.Key))
        {
            sb.AppendLine($"-- Round {round.Key} --");
            foreach (var answer in round.OrderBy(a => a.QuestionIndex))
            {
                sb.AppendLine($"Q: {answer.QuestionText}");
                sb.AppendLine($"A: {(string.IsNullOrWhiteSpace(answer.AnswerText) ? answer.DefaultHint : answer.AnswerText)}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
