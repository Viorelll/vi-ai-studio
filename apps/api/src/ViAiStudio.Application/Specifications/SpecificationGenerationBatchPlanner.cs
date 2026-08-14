using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record BatchPlan(
    int BatchIndex, string Name, bool Skip, string? SkipReason, IReadOnlyDictionary<string, string> Placeholders);

/// <summary>
/// Decides, from the intake sheet, which of the ten generation batches
/// actually produce files -- batch 5 needs an HTTP API deployable, batch 6
/// needs a web frontend, batch 7 (remaining deployables) is skipped
/// entirely when nothing is left to cover beyond backend/frontend -- and
/// renders each batch's {{placeholder}} values for its prompt template.
/// </summary>
public static class SpecificationGenerationBatchPlanner
{
    private static readonly (int Index, string Name)[] Batches =
    [
        (1, "01-meta"),
        (2, "02-product"),
        (3, "03-architecture"),
        (4, "04-database"),
        (5, "05-backend"),
        (6, "06-frontend"),
        (7, "07-remaining-deployables"),
        (8, "08-infrastructure"),
        (9, "09-quality"),
        (10, "10-delivery"),
    ];

    public static IReadOnlyList<BatchPlan> Plan(SpecificationIntakeSheet intake)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["deployables"] = string.Join(", ", intake.Deployables),
            ["functional_areas"] = string.Join(", ", intake.FunctionalAreas),
            ["frontend_requirements"] = string.Join(", ", intake.FrontendRequirements),
            ["environments"] = string.Join(", ", intake.Environments),
            ["compliance"] = string.Join(", ", intake.Compliance),
            ["rigour"] = intake.Rigour,
            ["team"] = intake.Team,
            ["spec_scope"] = intake.SpecScope,
        };

        var plans = new List<BatchPlan>();
        foreach (var (index, name) in Batches)
        {
            var (skip, reason) = ShouldSkip(index, intake);
            plans.Add(new BatchPlan(index, name, skip, reason, placeholders));
        }
        return plans;
    }

    private static (bool Skip, string? Reason) ShouldSkip(int batchIndex, SpecificationIntakeSheet intake)
    {
        if (batchIndex == 5 && !intake.Deployables.Contains("HTTP API"))
        {
            return (true, "No HTTP API deployable selected.");
        }
        if (batchIndex == 6 && (intake.Frontend == "no UI (API only)" || !intake.Deployables.Contains("web SPA")))
        {
            return (true, "No web frontend selected.");
        }
        if (batchIndex == 7)
        {
            var remaining = intake.Deployables.Where(d => d != "HTTP API" && d != "web SPA").ToList();
            if (remaining.Count == 0)
            {
                return (true, "No deployables beyond the backend/frontend selected.");
            }
        }
        return (false, null);
    }
}
