using System.Text;
using System.Text.RegularExpressions;
using ViAiStudio.AiGenerator.Contracts;

namespace ViAiStudio.AiGenerator.Generation;

public enum CoverageStatus
{
    /// <summary>Evidence of an implementation was found.</summary>
    Implemented,

    /// <summary>
    /// The specification states nothing this check can look for -- no declared
    /// output paths, no entity or route names. Reported separately rather than
    /// counted as implemented, because claiming to have verified something
    /// unverifiable is the one answer that helps nobody.
    /// </summary>
    Unverifiable,

    /// <summary>The specification is checkable, and no trace of it exists in the project.</summary>
    Missing,
}

/// <summary>Whether one specification can be shown to have been implemented, and what showed it.</summary>
public sealed record SpecCoverage(SpecDocumentDto Spec, CoverageStatus Status, string Evidence);

/// <summary>
/// The result of checking a generated project against the specification it was
/// built from.
/// </summary>
public sealed record SpecCoverageReport(IReadOnlyList<SpecCoverage> Results)
{
    public IReadOnlyList<SpecCoverage> Implemented => Results.Where(r => r.Status == CoverageStatus.Implemented).ToList();
    public IReadOnlyList<SpecCoverage> Unverifiable => Results.Where(r => r.Status == CoverageStatus.Unverifiable).ToList();

    /// <summary>The actionable set: specifications the gap-filling loop asks the model to implement.</summary>
    public IReadOnlyList<SpecCoverage> Missing => Results.Where(r => r.Status == CoverageStatus.Missing).ToList();

    public int Total => Results.Count;

    /// <summary>
    /// The share of specifications not known to be missing. Unverifiable specs
    /// count here -- they cannot be evidenced either way, so failing a build
    /// over them would make the threshold unreachable rather than meaningful.
    /// </summary>
    public int PercentComplete => Total == 0 ? 100 : (int)Math.Round((Total - Missing.Count) * 100.0 / Total);

    public string Summary => Unverifiable.Count == 0
        ? $"{Implemented.Count}/{Total} implemented, {Missing.Count} missing"
        : $"{Implemented.Count}/{Total} implemented, {Missing.Count} missing, "
          + $"{Unverifiable.Count} with nothing to check against";

    /// <summary>Renders the gaps as the failure output the repair/gap-filling loop reads.</summary>
    public string DescribeGaps()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Specification coverage: {Summary}.");
        sb.AppendLine($"{Missing.Count} specification(s) have no corresponding implementation in the generated project:");
        foreach (var gap in Missing)
        {
            sb.AppendLine($"- {Describe(gap.Spec)} — {gap.Evidence}");
        }
        return sb.ToString();
    }

    private static string Describe(SpecDocumentDto spec) =>
        string.IsNullOrWhiteSpace(spec.SpecId) ? spec.Path : $"{spec.SpecId} ({spec.Path}) “{spec.Title}”";
}

/// <summary>
/// Checks that the generated repository actually implements the specification,
/// rather than merely compiling and booting.
///
/// The Docker verification steps answer "does this project build and run?" --
/// a project implementing three of a hundred specifications passes all of them
/// happily. This closes that gap by looking for evidence of each specification
/// in the generated files, so a build can only be called done once the
/// specification it was built from is genuinely covered.
///
/// Evidence is deliberately structural rather than semantic: a specification's
/// declared <c>generates</c> paths, or the distinctive identifiers it names
/// (entities, routes, screens). This cannot prove the implementation is
/// *correct* -- that is what the generated tests and the integration run are
/// for -- but it reliably catches the failure this pipeline actually suffers
/// from, which is a specification never being implemented at all.
/// </summary>
public static class SpecCoverageValidator
{
    public static SpecCoverageReport Analyze(
        IReadOnlyList<SpecDocumentDto> specs, IReadOnlyList<GeneratedFile> files)
    {
        var checkable = specs.Where(ProjectBuildPlanner.IsCoverageCheckable).ToList();
        var index = new ProjectIndex(files);

        return new SpecCoverageReport(checkable.Select(spec => Check(spec, index)).ToList());
    }

    private static SpecCoverage Check(SpecDocumentDto spec, ProjectIndex index)
    {
        // A specification that declares which files it produces is the
        // strongest signal available -- the author said what to look for.
        var declared = (spec.Generates ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        foreach (var pattern in declared)
        {
            var match = index.FindByDeclaredPath(pattern);
            if (match is not null)
            {
                return new SpecCoverage(spec, CoverageStatus.Implemented, $"declares `{pattern}`, matched by `{match}`");
            }
        }

        // Otherwise fall back to the identifiers the specification names.
        // Requiring every identifier would flag specs whose vocabulary simply
        // differs from the code's; requiring a clear majority catches the
        // "never implemented" case without punishing paraphrase.
        var identifiers = ExtractIdentifiers(spec);
        if (identifiers.Count > 0)
        {
            var found = identifiers.Where(index.MentionsIdentifier).ToList();
            var required = Math.Max(1, (int)Math.Ceiling(identifiers.Count / 2.0));

            if (found.Count >= required)
            {
                return new SpecCoverage(spec, CoverageStatus.Implemented,
                    $"{found.Count}/{identifiers.Count} of its identifiers appear in the code ({string.Join(", ", found.Take(4))})");
            }

            var absent = identifiers.Except(found, StringComparer.OrdinalIgnoreCase).Take(6).ToList();
            return new SpecCoverage(spec, CoverageStatus.Missing,
                declared.Count > 0
                    ? $"none of its declared paths exist ({string.Join(", ", declared.Take(3))}) and its key identifiers are absent from the code: {string.Join(", ", absent)}"
                    : $"its key identifiers are absent from the code: {string.Join(", ", absent)}");
        }

        if (declared.Count > 0)
        {
            return new SpecCoverage(spec, CoverageStatus.Missing,
                $"none of its declared paths exist in the project: {string.Join(", ", declared.Take(4))}");
        }

        // Nothing checkable was stated. Treating this as a gap would put the
        // gap-filling loop to work on a specification it can never satisfy, so
        // it is reported as unverifiable rather than either passed or failed.
        return new SpecCoverage(spec, CoverageStatus.Unverifiable,
            "states no output paths, entities or routes to check for");
    }

    /// <summary>
    /// Pulls out the terms that must survive into code if the specification
    /// was implemented: the entities and tables it declares in its headings,
    /// and the routes it defines.
    ///
    /// Only these two forms count. Words taken from a specification's *title*
    /// were tried and removed: they yield generic English ("System", "Schema",
    /// "Backup", "Working") whose presence or absence in a codebase says
    /// nothing, and they produced every false gap in testing against the
    /// reference specification. A specification with no strong identifier is
    /// reported as unverifiable rather than judged on noise.
    /// </summary>
    private static List<string> ExtractIdentifiers(SpecDocumentDto spec)
    {
        var identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // `# Entity: `Project`` / `**Table:** `projects`` -- the backticked
        // names the document templates prescribe for entities and tables.
        foreach (Match match in Regex.Matches(
            spec.Content, @"^(?:#{1,3}\s+|\*\*(?:Table|Schema|Entity)[^*]*\*\*[^`\n]*)`([A-Za-z][A-Za-z0-9_]{2,})`",
            RegexOptions.Multiline))
        {
            identifiers.Add(match.Groups[1].Value);
        }

        // Route segments: `/api/v1/projects` -> "projects". The route is the
        // one thing an endpoint spec and its implementation must share exactly.
        foreach (Match match in Regex.Matches(spec.Content, @"`?/api/[A-Za-z0-9_/{}.-]*`?"))
        {
            foreach (var segment in match.Value.Trim('`').Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment is "api" || segment.StartsWith('{')) continue;
                if (Regex.IsMatch(segment, @"^v\d+$")) continue;
                if (segment.Length > 2) identifiers.Add(segment);
            }
        }

        foreach (var noise in NoiseWords) identifiers.Remove(noise);

        // Cap it: a handful of strong signals beats a long tail of weak ones,
        // and the majority rule above would otherwise be dominated by noise.
        return identifiers.Take(12).ToList();
    }

    /// <summary>
    /// Words that appear in specification prose and in virtually any codebase,
    /// so their presence proves nothing either way.
    /// </summary>
    private static readonly string[] NoiseWords =
    [
        "The", "This", "And", "For", "With", "From", "That", "When", "Then", "Given",
        "Spec", "Specification", "Overview", "Index", "Model", "Models", "Rules", "Notes",
        "Purpose", "Summary", "Response", "Request", "Error", "Errors", "Status", "String",
        "Type", "Types", "Value", "Values", "Name", "Table", "Column", "Columns", "Http",
    ];

    /// <summary>
    /// The generated repository, indexed for the two questions coverage asks:
    /// does a path like this exist, and does this word appear anywhere in the
    /// code. Both are asked once per specification, so the content is
    /// concatenated up front rather than rescanned per lookup.
    /// </summary>
    private sealed class ProjectIndex
    {
        private readonly IReadOnlyList<string> paths;
        private readonly string allContent;

        public ProjectIndex(IReadOnlyList<GeneratedFile> files)
        {
            paths = files.Select(f => f.Path.Replace('\\', '/')).ToList();

            var sb = new StringBuilder();
            foreach (var file in files)
            {
                sb.Append(file.Path).Append('\n').Append(file.Content).Append('\n');
            }
            allContent = sb.ToString();
        }

        /// <summary>
        /// Matches a specification's declared output path against the project.
        ///
        /// Declared paths are written in the specification's own vocabulary
        /// (`src/Infrastructure/Tenancy/**`), while this pipeline mandates a
        /// fixed `backend/`+`frontend/` layout, so the two rarely share a
        /// prefix. Matching therefore uses the distinctive tail of the pattern
        /// -- the part that carries the meaning -- rather than the full path.
        /// </summary>
        public string? FindByDeclaredPath(string pattern)
        {
            var normalized = pattern.Replace('\\', '/').Trim().Trim('"', '\'').TrimStart('/');

            // A bare extension glob such as "*.slnx".
            if (normalized.StartsWith("*."))
            {
                var extension = normalized[1..];
                return paths.FirstOrDefault(p => p.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
            }

            var withoutGlob = normalized
                .Replace("/**", "", StringComparison.Ordinal)
                .Replace("**", "", StringComparison.Ordinal)
                .Trim('/');

            if (withoutGlob.Length == 0) return null;

            // A concrete file name (possibly with a directory that won't match
            // our layout): the file name alone is the reliable signal.
            if (!withoutGlob.Contains('*'))
            {
                var fileName = System.IO.Path.GetFileName(withoutGlob);
                if (!string.IsNullOrEmpty(fileName) && fileName.Contains('.'))
                {
                    var byName = paths.FirstOrDefault(p =>
                        System.IO.Path.GetFileName(p).Equals(fileName, StringComparison.OrdinalIgnoreCase));
                    if (byName is not null) return byName;
                }
            }

            // A directory pattern: keep the last two segments, which name the
            // concern ("Infrastructure/Tenancy"), and look for them anywhere.
            var segments = withoutGlob.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !s.Contains('*'))
                .ToList();
            if (segments.Count == 0) return null;

            var tail = string.Join('/', segments.TakeLast(2));
            var byTail = paths.FirstOrDefault(p => p.Contains(tail, StringComparison.OrdinalIgnoreCase));
            if (byTail is not null) return byTail;

            // Last resort: the single most specific segment, provided it is
            // distinctive enough that a coincidental hit is unlikely.
            var leaf = segments[^1];
            return leaf.Length >= 4
                ? paths.FirstOrDefault(p => p.Contains(leaf, StringComparison.OrdinalIgnoreCase))
                : null;
        }

        public bool MentionsIdentifier(string identifier) =>
            allContent.Contains(identifier, StringComparison.OrdinalIgnoreCase);
    }
}
