using ViAiStudio.AiGenerator.Contracts;

namespace ViAiStudio.AiGenerator.Generation;

/// <summary>
/// One model call's worth of the build: the specifications it is responsible
/// for turning into code, plus the goal text that tells the model what part of
/// the repository it owns this round.
/// </summary>
public sealed record BuildPhase(
    int Index,
    string Name,
    string Goal,
    IReadOnlyList<SpecDocumentDto> Specs)
{
    /// <summary>Total specification characters this phase sends, used to keep prompts bounded.</summary>
    public int SpecCharacters => Specs.Sum(s => s.Content.Length);

    public string SpecIdRange => Specs.Count == 0
        ? "-"
        : string.Join(", ", Specs.Select(s => string.IsNullOrWhiteSpace(s.SpecId) ? s.Path : s.SpecId).Take(6))
          + (Specs.Count > 6 ? $" (+{Specs.Count - 6} more)" : "");
}

/// <summary>
/// Splits an authored specification into the ordered phases a build actually
/// generates in.
///
/// A real specification is far too large to implement in one model call -- the
/// reference specification is ~420,000 characters across 123 documents, and no
/// model can emit a repository implementing all of it in a single reply. The
/// old single-shot prompt therefore produced a thin skeleton that satisfied
/// the compiler and ignored almost every document. Phasing is what makes "all
/// specifications are implemented" achievable at all: each phase carries a
/// bounded slice of the specification in full, and the workspace accumulates
/// the files across phases.
///
/// Phases are ordered so that each one only depends on code earlier phases
/// have already written -- schema before the services that query it, services
/// before the endpoints that expose them, endpoints before the frontend that
/// calls them.
/// </summary>
public static class ProjectBuildPlanner
{
    /// <summary>
    /// Ceiling on the specification text in a single phase. Chosen so the
    /// phase's specs, the running file index and the layout contract all fit
    /// comfortably in the prompt while leaving the model most of its output
    /// budget for actual code. A phase whose specs exceed this is split into
    /// numbered slices rather than truncated -- dropping a document here would
    /// silently reintroduce exactly the "spec never got implemented" problem
    /// phasing exists to solve.
    /// </summary>
    private const int MaxSpecCharactersPerPhase = 60_000;

    /// <summary>
    /// Product-level specs (vision, personas, functional/non-functional
    /// requirements) describe *what* the product must do rather than which
    /// files to write. They are cross-cutting, so instead of being one phase
    /// they ride along with every phase as shared context.
    /// </summary>
    private const int MaxContextCharacters = 24_000;

    private sealed record PhaseDefinition(string Name, string Goal, Func<SpecDocumentDto, bool> Selects);

    /// <summary>
    /// The fixed spine of a build. Selection is by component/path prefix and
    /// spec-id prefix (see the Api's "generation.id-scheme" template), with
    /// each document landing in exactly the first phase that claims it.
    /// </summary>
    private static readonly PhaseDefinition[] Definitions =
    [
        new("Foundation",
            "Create the solution skeleton: the runnable backend host project at `backend/`, its csproj with pinned "
            + "packages, Program.cs wiring (OpenAPI, health endpoint, configuration), shared conventions, the root "
            + "`docker-compose.yml` and `README.md`. Establish the layering, error model and configuration approach "
            + "the later phases build on.",
            spec => HasPrefix(spec, "01-architecture") || HasId(spec, "ARCH", "ADR")),

        new("Database",
            "Implement the data tier: entity classes, the EF Core DbContext, configurations, relationships, indexes, "
            + "constraints and the migration/schema-creation path. The backend must create the real schema on startup "
            + "against an empty database.",
            spec => HasPrefix(spec, "02-database") || HasId(spec, "DB")),

        new("Backend services",
            "Implement backend concerns behind the endpoints: authentication, authorization, tenant resolution, "
            + "validation, error handling, caching, storage, notifications, health checks and observability, as the "
            + "specifications describe them.",
            spec => HasPrefix(spec, "03-apps/backend") && !HasPrefix(spec, "03-apps/backend/endpoints")
                    || HasId(spec, "BE")),

        new("API endpoints",
            "Implement every endpoint group: routes, request/response contracts, validation rules and error codes "
            + "exactly as specified. Each endpoint's documented acceptance criteria must hold.",
            spec => HasPrefix(spec, "03-apps/backend/endpoints") || HasId(spec, "API")),

        new("Frontend",
            "Implement the frontend at `frontend/`: screens, routing, state, API client and styling per the "
            + "specifications, with a `package.json` whose `build` script succeeds non-interactively.",
            spec => HasPrefix(spec, "03-apps/frontend") || HasId(spec, "FE", "UI")),

        new("Additional services",
            "Implement the remaining deployables described by the specifications -- scheduled jobs, messaging "
            + "workers and any other background service -- wired into the same solution.",
            spec => HasPrefix(spec, "03-apps") || HasId(spec, "SCH", "MSG")),

        new("Infrastructure",
            "Complete the deployment surface: Dockerfiles, the compose wiring for backend/frontend/database, "
            + "environment configuration and any supporting infrastructure the specifications call for.",
            spec => HasPrefix(spec, "04-infrastructure") || HasId(spec, "INF")),

        new("Quality and tests",
            "Implement the automated tests the quality specifications require, as a test project inside `backend/` "
            + "that `dotnet test` discovers and runs. Cover the acceptance criteria stated in the endpoint, entity "
            + "and backend specifications.",
            spec => HasPrefix(spec, "05-quality") || HasPrefix(spec, "06-delivery") || HasId(spec, "QA", "DEL")),
    ];

    /// <summary>
    /// Builds the ordered phase list for one specification. Phases whose
    /// specifications are absent are dropped -- a product with no frontend
    /// specs gets no frontend phase, rather than a phase that prompts the
    /// model to invent one. Oversized phases are split into slices so the
    /// prompt stays bounded no matter how large the specification grew.
    /// </summary>
    public static IReadOnlyList<BuildPhase> Plan(IReadOnlyList<SpecDocumentDto> documents)
    {
        var codeBearing = documents.Where(IsCodeBearing).ToList();
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var phases = new List<BuildPhase>();

        foreach (var definition in Definitions)
        {
            var specs = codeBearing
                .Where(spec => !claimed.Contains(spec.Path) && definition.Selects(spec))
                .OrderBy(spec => spec.Path, StringComparer.Ordinal)
                .ToList();

            if (specs.Count == 0) continue;

            foreach (var spec in specs) claimed.Add(spec.Path);

            foreach (var slice in Slice(specs))
            {
                phases.Add(new BuildPhase(phases.Count + 1, definition.Name, definition.Goal, slice));
            }
        }

        // Anything the definitions did not claim still has to be built -- an
        // unrecognised component is a specification the author wrote and
        // expects to see implemented, not one to quietly drop.
        var unclaimed = codeBearing.Where(spec => !claimed.Contains(spec.Path)).ToList();
        foreach (var slice in Slice(unclaimed))
        {
            phases.Add(new BuildPhase(
                phases.Count + 1,
                "Remaining specifications",
                "Implement the remaining specifications below, which do not belong to any standard component group.",
                slice));
        }

        // Renumber so indexes stay contiguous and match what the user is shown.
        return phases.Select((phase, index) => phase with { Index = index + 1 }).ToList();
    }

    /// <summary>
    /// The product-level specifications every phase gets as background. Kept
    /// compact: these say what the product is for, and repeating them in full
    /// in every phase would crowd out the specs the phase actually implements.
    /// </summary>
    public static IReadOnlyList<SpecDocumentDto> SelectSharedContext(IReadOnlyList<SpecDocumentDto> documents)
    {
        var context = documents
            .Where(spec => HasPrefix(spec, "00-product") || HasId(spec, "PRD", "FR", "NFR"))
            .OrderBy(spec => spec.Path, StringComparer.Ordinal)
            .ToList();

        var selected = new List<SpecDocumentDto>();
        var budget = MaxContextCharacters;
        foreach (var spec in context)
        {
            if (budget - spec.Content.Length < 0) continue;
            selected.Add(spec);
            budget -= spec.Content.Length;
        }
        return selected;
    }

    /// <summary>
    /// Splits a phase's documents into consecutive groups under the character
    /// budget. A single document larger than the budget still gets its own
    /// group rather than being dropped -- sending one oversized spec is far
    /// better than never implementing it.
    /// </summary>
    private static IEnumerable<IReadOnlyList<SpecDocumentDto>> Slice(IReadOnlyList<SpecDocumentDto> specs)
    {
        if (specs.Count == 0) yield break;

        var current = new List<SpecDocumentDto>();
        var size = 0;

        foreach (var spec in specs)
        {
            if (current.Count > 0 && size + spec.Content.Length > MaxSpecCharactersPerPhase)
            {
                yield return current;
                current = [];
                size = 0;
            }
            current.Add(spec);
            size += spec.Content.Length;
        }

        if (current.Count > 0) yield return current;
    }

    /// <summary>
    /// Whether a specification should be fed to a generation phase. `_meta/`
    /// documents are the authoring rules, templates and traceability matrix
    /// for the specification itself -- they describe how specs are written,
    /// not what the product does. Product specs are context rather than a
    /// phase of their own (see <see cref="SelectSharedContext"/>).
    /// </summary>
    public static bool IsCodeBearing(SpecDocumentDto spec) =>
        !HasPrefix(spec, "_meta") && !HasId(spec, "META")
        && !HasPrefix(spec, "00-product") && !HasId(spec, "PRD", "FR", "NFR")
        && !IsIndexDocument(spec);

    /// <summary>
    /// Whether a specification can meaningfully be checked for an
    /// implementation. This is narrower than <see cref="IsCodeBearing"/>:
    /// decision records are generated *from* -- they constrain how everything
    /// else is built -- but they describe a choice, not a unit of work, so
    /// there is no artifact to find. Checking them produces guaranteed false
    /// gaps, and sometimes inverted ones: an ADR titled "React + Vite rather
    /// than Blazor" would be reported missing precisely because the code
    /// correctly contains no Blazor.
    /// </summary>
    public static bool IsCoverageCheckable(SpecDocumentDto spec) =>
        IsCodeBearing(spec) && !IsDecisionRecord(spec);

    private static bool IsDecisionRecord(SpecDocumentDto spec) =>
        HasId(spec, "ADR")
        || NormalizePath(spec.Path).Contains("/adr/", StringComparison.OrdinalIgnoreCase)
        || NormalizePath(spec.Path).StartsWith("adr/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Manifests, changelogs and glossaries are rendered artefacts about the
    /// specification set, with no implementation of their own to check for.
    /// </summary>
    private static bool IsIndexDocument(SpecDocumentDto spec)
    {
        var fileName = System.IO.Path.GetFileName(NormalizePath(spec.Path));
        return fileName.Equals("manifest.md", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("changelog.md", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("glossary.md", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("readme.md", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasPrefix(SpecDocumentDto spec, string prefix) =>
        NormalizePath(spec.Path).StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
        || NormalizePath(spec.Component).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static bool HasId(SpecDocumentDto spec, params string[] prefixes) =>
        !string.IsNullOrWhiteSpace(spec.SpecId)
        && prefixes.Any(prefix => spec.SpecId.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase));

    private static string NormalizePath(string path) => (path ?? "").Replace('\\', '/').TrimStart('/');
}
