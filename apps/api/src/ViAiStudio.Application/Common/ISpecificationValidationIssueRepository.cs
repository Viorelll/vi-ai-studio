using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface ISpecificationValidationIssueRepository
{
    Task<IReadOnlyList<SpecificationValidationIssue>> ListAsync(Guid specificationId, CancellationToken cancellationToken);
}
