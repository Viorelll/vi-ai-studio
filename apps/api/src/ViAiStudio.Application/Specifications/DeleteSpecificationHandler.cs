using ViAiStudio.Application.Common;

namespace ViAiStudio.Application.Specifications;

public sealed class DeleteSpecificationHandler(ISpecificationRepository specificationRepository)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var specification = await specificationRepository.GetAsync(id, cancellationToken);
        if (specification is null)
        {
            return false;
        }

        await specificationRepository.RemoveAsync(specification, cancellationToken);
        await specificationRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
