using System.Text.RegularExpressions;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Catalog;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record GeneratePhaseChipsCommand(Guid SpecificationId, int PhaseIndex, string StepName);

/// <summary>
/// Drives the wizard's keyword-chip suggestions: asks the model routed for
/// Spec generation (falling back to Code generation) for a handful of
/// project-specific functionality suggestions for the step currently in
/// view, which the user can then toggle on as keywords for that phase.
/// </summary>
public sealed partial class GeneratePhaseChipsHandler(
    ISpecificationRepository specificationRepository,
    SpecGenerationModelResolver modelResolver,
    IAiCallLogRepository aiCallLogRepository,
    IAiGeneratorClient aiGeneratorClient)
{
    private const int ChipCount = 10;

    private const string SystemPrompt =
        "You suggest specific functionality keywords for one step of a software " +
        "project. Respond with exactly one short keyword phrase per line (3-6 words), " +
        "no numbering, no bullets, no headings, no extra commentary.";

    public async Task<IReadOnlyList<string>> HandleAsync(GeneratePhaseChipsCommand command, CancellationToken cancellationToken)
    {
        var definition = SpecificationPhaseCatalog.ByIndex(command.PhaseIndex);

        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        var config = await modelResolver.ResolveAsync(cancellationToken);
        var prompt = BuildPrompt(specification, definition, command.StepName);

        var generated = await aiGeneratorClient.GenerateTextAsync(
            ModelCredentials.FromConfig(config), SystemPrompt, prompt, cancellationToken);

        var chips = ParseChips(generated.Text);

        await aiCallLogRepository.AddAsync(new AiCallLog
        {
            Id = Guid.NewGuid(),
            SpecificationId = specification.Id,
            GenerationVersion = null,
            Task = AiTaskType.SpecGeneration,
            Model = config.Label,
            TokensIn = generated.TokensIn,
            TokensOut = generated.TokensOut,
            Prompt = prompt,
            Result = generated.Text,
            CreatedAt = DateTimeOffset.UtcNow,
        }, cancellationToken);
        await aiCallLogRepository.SaveChangesAsync(cancellationToken);

        return chips;
    }

    private static string BuildPrompt(Specification specification, SpecificationPhaseDefinition definition, string stepName)
    {
        if (definition.Index == 1 && stepName == "Functional Requirements")
        {
            return BuildFunctionalRequirementsPrompt(specification);
        }

        if (definition.Index == 1 && stepName == "Non-functional Requirements")
        {
            return BuildNonFunctionalRequirementsPrompt(specification);
        }

        var stack = specification.Stack;
        var lines = new List<string>
        {
            $"Project: {specification.Name}",
            $"Summary: {specification.Summary}",
            $"Stack: backend {stack.Backend}, UI {stack.Ui}, database {stack.Database}, " +
                $"containerization {stack.Infra}, UI style {stack.UiStyle}",
            $"Phase: {definition.Title}",
            $"Step: {stepName}",
            $"List the top {ChipCount} most relevant, specific functionalities for this step of " +
                "this exact project. Tailor them to what this specific project actually needs, not a " +
                "generic checklist -- similar in spirit to examples like Authentication, User & profile " +
                "management, or CRUD/querying/filtering & pagination, but specific to this project's domain.",
        };
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Functional Requirements chips deliberately ignore the tech stack -- they're
    /// about what the product *does* (its domain actions, users, pages), which
    /// follows from the name and summary alone regardless of how it's built.
    /// </summary>
    private static string BuildFunctionalRequirementsPrompt(Specification specification)
    {
        var lines = new List<string>
        {
            $"Project: {specification.Name}",
            $"Summary: {specification.Summary}",
            $"List the top {ChipCount} functional requirements for this exact project, based only on its " +
                "name and summary above -- the technology stack doesn't matter here. Tailor them to this " +
                "project's specific domain, not a generic checklist: think in terms of the concrete actions " +
                "and capabilities users need, e.g. create/update/delete/read the main subject of the app, " +
                "user accounts, a landing page, and similar functional building blocks specific to this project.",
        };
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Non-functional Requirements chips are the technical counterpart -- unlike
    /// Functional Requirements, these are meant to be shaped by the chosen stack
    /// and the database entities/schema it implies.
    /// </summary>
    private static string BuildNonFunctionalRequirementsPrompt(Specification specification)
    {
        var stack = specification.Stack;
        var lines = new List<string>
        {
            $"Project: {specification.Name}",
            $"Summary: {specification.Summary}",
            $"Stack: backend {stack.Backend}, UI {stack.Ui}, database {stack.Database}, " +
                $"containerization {stack.Infra}, UI style {stack.UiStyle}",
            $"List the top {ChipCount} non-functional requirements for this exact project, given its name, " +
                "summary, and technology stack above. Keep in mind the database entities/schema this domain " +
                "implies and technical concerns like performance, scalability, security, data integrity, and " +
                "integration between the backend, UI, and database. Tailor them to this specific project and " +
                "stack, not a generic checklist.",
        };
        return string.Join('\n', lines);
    }

    private static IReadOnlyList<string> ParseChips(string text) =>
        text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => ListMarker().Replace(line, "").Trim())
            .Where(line => line.Length > 0)
            .Take(ChipCount)
            .ToList();

    [GeneratedRegex(@"^\s*(?:[-*•]|\d+[.)])\s*")]
    private static partial Regex ListMarker();
}
