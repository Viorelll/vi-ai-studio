using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;
using ViAiStudio.Domain.ValueObjects;

namespace ViAiStudio.Application.Specifications;

public sealed record CreateSpecificationCommand(string Name, string Summary, string Owner, TechStack? Stack = null);

/// <summary>Creates a Draft specification, ready for stage 1 (chip selection).</summary>
public sealed class CreateSpecificationHandler(ISpecificationRepository specificationRepository)
{
    public async Task<Specification> HandleAsync(CreateSpecificationCommand command, CancellationToken cancellationToken)
    {
        var specification = new Specification
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Summary = command.Summary,
            Owner = command.Owner,
            Status = SpecificationStatus.Draft,
            Progress = 0,
            Stack = command.Stack ?? TechStack.Default,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await specificationRepository.AddAsync(specification, cancellationToken);
        await specificationRepository.SaveChangesAsync(cancellationToken);

        return specification;
    }
}
