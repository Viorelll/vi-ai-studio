using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

/// <summary>
/// Deterministic (not AI) evaluation of what a stage-1 chip selection forces,
/// what it contradicts, and what it still leaves unknown -- the intake
/// sheet's implied_decisions/conflicts_resolved/still_unknown fields.
/// Mirrors the "Changes:" notes already carried by each chip group.
/// </summary>
public static class IntakeConflictRules
{
    public static (List<string> Implied, List<string> Conflicts, List<string> StillUnknown) Evaluate(SpecificationIntakeSheet intake)
    {
        var implied = new List<string>();
        var conflicts = new List<string>();

        if (!intake.Deployables.Contains("scheduler host"))
        {
            implied.Add("No 03-apps/scheduler folder will be generated -- no scheduled/recurring job specs.");
        }
        if (!intake.Deployables.Contains("message worker"))
        {
            implied.Add("No 03-apps/service-bus-worker folder will be generated -- integration events and messaging specs are skipped.");
        }
        if (intake.Frontend == "no UI (API only)")
        {
            implied.Add("No 03-apps/frontend folder will be generated.");
        }
        if (intake.TenantIsolation == "single tenant")
        {
            implied.Add("Multi-tenancy architecture and row-level security specs are simplified: there is only ever one tenant.");
        }

        if (intake.TenantIsolation != "single tenant" && !intake.FunctionalAreas.Contains("multi-tenancy"))
        {
            conflicts.Add("Tenant isolation model implies multi-tenancy; \"multi-tenancy\" was added to the selected functional areas.");
            intake.FunctionalAreas = [.. intake.FunctionalAreas, "multi-tenancy"];
        }

        if (intake.Deployables.Contains("message worker") && intake.SupportingInfrastructure.Count == 0)
        {
            conflicts.Add("A message worker was selected with no message broker; RabbitMQ was assumed as supporting infrastructure.");
            intake.SupportingInfrastructure = [.. intake.SupportingInfrastructure, "RabbitMQ"];
        }

        if (intake.Compliance.Contains("HIPAA") || intake.Compliance.Contains("PCI via provider"))
        {
            if (intake.Rigour == "prototype")
            {
                conflicts.Add("Selected compliance requirements (HIPAA/PCI) are incompatible with \"prototype\" rigour; security and DR specs will be written at production-ready strictness regardless.");
            }
        }

        var stillUnknown = new List<string>
        {
            "the domain's core entities and their ownership relationships",
            "the role catalogue and which actions are dangerous",
            "at least three end-to-end journeys, including their unhappy paths",
            "invariants -- what must never or must always be true",
            "expected scale and a concrete latency budget",
            "external systems and which of them may fail",
            "recovery expectations (tolerable data loss, tolerable downtime)",
        };

        return (implied, conflicts, stillUnknown);
    }
}
