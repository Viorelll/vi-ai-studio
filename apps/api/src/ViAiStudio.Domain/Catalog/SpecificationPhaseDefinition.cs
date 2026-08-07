namespace ViAiStudio.Domain.Catalog;

/// <summary>One step of the fixed 15-phase AI Specification Studio wizard.</summary>
public sealed record SpecificationPhaseDefinition(int Index, string Title, IReadOnlyList<string> Items, string Output);
