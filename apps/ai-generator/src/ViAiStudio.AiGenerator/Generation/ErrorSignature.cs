using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ViAiStudio.AiGenerator.Generation;

/// <summary>
/// Normalizes a failure log into a short, comparable fingerprint. A repair
/// loop that keeps producing different files but the identical failure is
/// making no progress -- it should stop, not burn the rest of its attempt
/// budget restating the same error. Comparing raw output would never match
/// twice in a row (timestamps, container IDs, temp paths all change between
/// runs), so this keeps only the compiler/lint diagnostic codes, which are
/// stable across reruns of the same underlying problem.
/// </summary>
public static partial class ErrorSignature
{
    // dotnet/MSBuild diagnostic codes: CS0246, TS2339, NU1101, MSB3073.
    [GeneratedRegex(@"\b[A-Z]{2,5}\d{2,5}\b", RegexOptions.Compiled)]
    private static partial Regex CompilerDiagnosticCode();

    // ESLint's default "stylish" formatter: "  12:5  error  'x' is not defined  no-undef" --
    // the rule id is the last column, separated by a run of spaces.
    [GeneratedRegex(@"^\s*\d+:\d+\s+(?:error|warning)\s+.*\S\s{2,}(?<rule>[\w@][\w@/-]{2,60})\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex LintRuleId();

    public static string Compute(string output)
    {
        var codes = CompilerDiagnosticCode().Matches(output).Select(m => m.Value)
            .Concat(LintRuleId().Matches(output).Select(m => m.Groups["rule"].Value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        // No recognizable diagnostic codes (e.g. a generic runtime crash with
        // free-form prose) -- fall back to a hash of the whole trimmed output
        // so two byte-identical failures still compare equal.
        var basis = codes.Count > 0 ? string.Join('|', codes) : output.Trim();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(basis)));
    }
}
