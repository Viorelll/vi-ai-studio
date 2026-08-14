using ViAiStudio.Application.Specifications;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Api.Contracts;

public sealed record ChipOptionResponse(string Value, bool IsDefault)
{
    public static ChipOptionResponse FromValue(ChipOption option) => new(option.Value, option.IsDefault);
}

public sealed record ChipGroupResponse(
    string Group, string Label, string SheetField, string SelectMode,
    IReadOnlyList<ChipOptionResponse> Options, string Changes)
{
    public static ChipGroupResponse FromValue(ChipGroup group) => new(
        group.Group, group.Label, group.SheetField, group.SelectMode,
        group.Options.Select(ChipOptionResponse.FromValue).ToList(), group.Changes);
}

public sealed record IntakeSheetResponse(
    string ProductShape,
    string TenantIsolation,
    IReadOnlyList<string> Deployables,
    string IdentityModel,
    IReadOnlyList<string> IdentityFeatures,
    string PrimaryDatabase,
    IReadOnlyList<string> SupportingInfrastructure,
    string Frontend,
    IReadOnlyList<string> FrontendRequirements,
    IReadOnlyList<string> FunctionalAreas,
    IReadOnlyList<string> Compliance,
    IReadOnlyList<string> Environments,
    string Rigour,
    string SpecScope,
    string Team,
    IReadOnlyList<string> ImpliedDecisions,
    IReadOnlyList<string> ConflictsResolved,
    IReadOnlyList<string> StillUnknown,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? InterviewCompletedAt)
{
    public static IntakeSheetResponse FromEntity(SpecificationIntakeSheet intake) => new(
        intake.ProductShape, intake.TenantIsolation, intake.Deployables,
        intake.IdentityModel, intake.IdentityFeatures, intake.PrimaryDatabase, intake.SupportingInfrastructure,
        intake.Frontend, intake.FrontendRequirements, intake.FunctionalAreas, intake.Compliance,
        intake.Environments, intake.Rigour, intake.SpecScope, intake.Team,
        intake.ImpliedDecisions, intake.ConflictsResolved, intake.StillUnknown,
        intake.CompletedAt, intake.InterviewCompletedAt);
}

public sealed record InterviewQuestionResponse(int Order, string Prompt, string DefaultHint)
{
    public static InterviewQuestionResponse FromValue(InterviewQuestion question) =>
        new(question.Order, question.Prompt, question.DefaultHint);
}

public sealed record InterviewRoundResponse(int Round, string Title, IReadOnlyList<InterviewQuestionResponse> Questions)
{
    public static InterviewRoundResponse FromValue(InterviewRound round) =>
        new(round.Round, round.Title, round.Questions.Select(InterviewQuestionResponse.FromValue).ToList());
}

public sealed record InterviewAnswerResponse(
    int RoundIndex, int QuestionIndex, string QuestionText, string DefaultHint, string? AnswerText, bool UsedDefault)
{
    public static InterviewAnswerResponse FromEntity(SpecificationInterviewAnswer answer) => new(
        answer.RoundIndex, answer.QuestionIndex, answer.QuestionText, answer.DefaultHint, answer.AnswerText, answer.UsedDefault);
}

public sealed record SaveInterviewAnswerRequest(int QuestionIndex, string QuestionText, string DefaultHint, string? AnswerText)
{
    public InterviewAnswerInput ToInput() => new(QuestionIndex, QuestionText, DefaultHint, AnswerText);
}

public sealed record SaveInterviewRoundRequest(List<SaveInterviewAnswerRequest> Answers);

public sealed record ExpandInterviewAnswerRequest(string QuestionText, string AnswerText);

public sealed record ExpandInterviewAnswerResponse(string ExpandedText);

public sealed record SaveIntakeChipsRequest(
    string ProductShape,
    string TenantIsolation,
    List<string> Deployables,
    string IdentityModel,
    List<string> IdentityFeatures,
    string PrimaryDatabase,
    List<string> SupportingInfrastructure,
    string Frontend,
    List<string> FrontendRequirements,
    List<string> FunctionalAreas,
    List<string> Compliance,
    List<string> Environments,
    string Rigour,
    string SpecScope,
    string Team)
{
    public SaveIntakeChipsCommand ToCommand(Guid specificationId) => new(
        specificationId, ProductShape, TenantIsolation, Deployables, IdentityModel, IdentityFeatures,
        PrimaryDatabase, SupportingInfrastructure, Frontend, FrontendRequirements, FunctionalAreas,
        Compliance, Environments, Rigour, SpecScope, Team);
}
