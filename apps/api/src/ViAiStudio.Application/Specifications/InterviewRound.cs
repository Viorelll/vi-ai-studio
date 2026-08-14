namespace ViAiStudio.Application.Specifications;

public sealed record InterviewQuestion(int Order, string Prompt, string DefaultHint);

/// <summary>One stage-2 round (see PromptLibrarySeedData/interview.round.*.json).</summary>
public sealed record InterviewRound(int Round, string Title, IReadOnlyList<InterviewQuestion> Questions);
