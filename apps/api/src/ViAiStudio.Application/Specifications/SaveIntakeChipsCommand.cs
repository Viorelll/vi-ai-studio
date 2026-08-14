using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record SaveIntakeChipsCommand(
    Guid SpecificationId,
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
    string Team);

/// <summary>Persists stage-1 chip selections, creating the intake sheet on the first save.</summary>
public sealed class SaveIntakeChipsHandler(ISpecificationRepository specificationRepository)
{
    public async Task<SpecificationIntakeSheet> HandleAsync(SaveIntakeChipsCommand command, CancellationToken cancellationToken)
    {
        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        var intake = specification.Intake;
        if (intake is null)
        {
            intake = new SpecificationIntakeSheet { Id = Guid.NewGuid(), SpecificationId = specification.Id };
            await specificationRepository.AddNewChildAsync(intake, cancellationToken);
            specification.Intake = intake;
        }

        intake.ProductShape = command.ProductShape;
        intake.TenantIsolation = command.TenantIsolation;
        intake.Deployables = command.Deployables;
        intake.IdentityModel = command.IdentityModel;
        intake.IdentityFeatures = command.IdentityFeatures;
        intake.PrimaryDatabase = command.PrimaryDatabase;
        intake.SupportingInfrastructure = command.SupportingInfrastructure;
        intake.Frontend = command.Frontend;
        intake.FrontendRequirements = command.FrontendRequirements;
        intake.FunctionalAreas = command.FunctionalAreas;
        intake.Compliance = command.Compliance;
        intake.Environments = command.Environments;
        intake.Rigour = command.Rigour;
        intake.SpecScope = command.SpecScope;
        intake.Team = command.Team;

        var (implied, conflicts, stillUnknown) = IntakeConflictRules.Evaluate(intake);
        intake.ImpliedDecisions = implied;
        intake.ConflictsResolved = conflicts;
        intake.StillUnknown = stillUnknown;
        intake.CompletedAt = DateTimeOffset.UtcNow;

        await specificationRepository.SaveChangesAsync(cancellationToken);
        return intake;
    }
}
