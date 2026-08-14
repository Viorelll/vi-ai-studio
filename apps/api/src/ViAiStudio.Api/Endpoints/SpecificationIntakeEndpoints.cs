using ViAiStudio.Api.Contracts;
using ViAiStudio.Application.Common;
using ViAiStudio.Application.Specifications;

namespace ViAiStudio.Api.Endpoints;

/// <summary>Stage 1 (chip selection) and stage 2 (domain interview) of the specification wizard.</summary>
public static class SpecificationIntakeEndpoints
{
    public static void MapSpecificationIntakeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/specifications/{id:guid}/intake").RequireAuthorization();

        group.MapGet("/chip-groups", async (ListChipGroupsHandler handler, CancellationToken cancellationToken) =>
        {
            var chipGroups = await handler.HandleAsync(cancellationToken);
            return Results.Ok(chipGroups.Select(ChipGroupResponse.FromValue));
        });

        group.MapGet("/", async (Guid id, ISpecificationRepository repository, CancellationToken cancellationToken) =>
        {
            var specification = await repository.GetAsync(id, cancellationToken);
            if (specification is null)
            {
                return Results.NotFound();
            }
            return Results.Ok(specification.Intake is null ? null : IntakeSheetResponse.FromEntity(specification.Intake));
        });

        group.MapPut("/chips", async (
            Guid id, SaveIntakeChipsRequest request, SaveIntakeChipsHandler handler, CancellationToken cancellationToken) =>
        {
            var intake = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
            return Results.Ok(IntakeSheetResponse.FromEntity(intake));
        });

        group.MapGet("/interview-rounds", async (ListInterviewRoundsHandler handler, CancellationToken cancellationToken) =>
        {
            var rounds = await handler.HandleAsync(cancellationToken);
            return Results.Ok(rounds.Select(InterviewRoundResponse.FromValue));
        });

        group.MapGet("/interview-answers", async (Guid id, ISpecificationRepository repository, CancellationToken cancellationToken) =>
        {
            var specification = await repository.GetAsync(id, cancellationToken);
            if (specification is null)
            {
                return Results.NotFound();
            }
            return Results.Ok(specification.InterviewAnswers
                .OrderBy(a => a.RoundIndex).ThenBy(a => a.QuestionIndex)
                .Select(InterviewAnswerResponse.FromEntity));
        });

        group.MapPut("/interview/{roundIndex:int}", async (
            Guid id, int roundIndex, SaveInterviewRoundRequest request, SaveInterviewRoundHandler handler, CancellationToken cancellationToken) =>
        {
            var command = new SaveInterviewRoundCommand(id, roundIndex, request.Answers.Select(a => a.ToInput()).ToList());
            var answers = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(answers.Select(InterviewAnswerResponse.FromEntity));
        });

        group.MapPost("/interview/expand", async (
            Guid id, ExpandInterviewAnswerRequest request, ExpandInterviewAnswerHandler handler, CancellationToken cancellationToken) =>
        {
            var expanded = await handler.HandleAsync(
                new ExpandInterviewAnswerCommand(id, request.QuestionText, request.AnswerText), cancellationToken);
            return Results.Ok(new ExpandInterviewAnswerResponse(expanded));
        });

        group.MapPost("/complete", async (
            Guid id, CompleteIntakeInterviewHandler handler, CancellationToken cancellationToken) =>
        {
            var intake = await handler.HandleAsync(new CompleteIntakeInterviewCommand(id), cancellationToken);
            return Results.Ok(IntakeSheetResponse.FromEntity(intake));
        });
    }
}
