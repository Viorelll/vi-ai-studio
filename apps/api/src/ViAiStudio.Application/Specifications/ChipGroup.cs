namespace ViAiStudio.Application.Specifications;

public sealed record ChipOption(string Value, bool IsDefault);

/// <summary>One stage-1 selection group (see PromptLibrarySeedData/chips.group.*.json).</summary>
public sealed record ChipGroup(
    string Group,
    string Label,
    string SheetField,
    string SelectMode,
    IReadOnlyList<ChipOption> Options,
    string Changes);
