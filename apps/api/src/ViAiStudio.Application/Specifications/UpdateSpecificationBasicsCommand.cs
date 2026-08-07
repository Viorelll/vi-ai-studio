using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;
using ViAiStudio.Domain.ValueObjects;

namespace ViAiStudio.Application.Specifications;

public sealed record UpdateSpecificationBasicsCommand(
    Guid SpecificationId,
    string? Name,
    string? Summary,
    string? Audience,
    TechStack? Stack);

public sealed class UpdateSpecificationBasicsHandler(ISpecificationRepository specificationRepository)
{
    public async Task<Specification> HandleAsync(UpdateSpecificationBasicsCommand command, CancellationToken cancellationToken)
    {
        var specification = await specificationRepository.GetAsync(command.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{command.SpecificationId}' does not exist.");

        if (specification.Status != SpecificationStatus.Draft)
        {
            throw new InvalidOperationException("Only a Draft specification's basics can be edited.");
        }

        if (command.Name is not null) specification.Name = command.Name;
        if (command.Summary is not null) specification.Summary = command.Summary;
        if (command.Audience is not null) specification.Audience = command.Audience;
        if (command.Stack is not null) specification.Stack = command.Stack;

        await specificationRepository.SaveChangesAsync(cancellationToken);
        return specification;
    }
}
