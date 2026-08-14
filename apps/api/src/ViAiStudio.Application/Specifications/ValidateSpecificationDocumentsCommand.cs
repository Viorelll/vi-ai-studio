using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

public sealed record ValidateSpecificationDocumentsCommand(Guid SpecificationId, Guid RunId);

/// <summary>
/// Runs after the last batch: checks duplicate spec IDs, dangling
/// `depends_on` references, dependency cycles, `generates` path collisions,
/// and missing acceptance-criteria sections. Ports the validation section of
/// specification-example's generate-manifest.py to C#. Non-blocking -- a
/// draft spec with listed problems is more useful than one that silently
/// invented an answer, so issues are recorded, never thrown.
/// </summary>
public sealed class ValidateSpecificationDocumentsHandler(
    ISpecificationDocumentRepository documentRepository,
    ISpecificationRepository specificationRepository)
{
    public async Task<IReadOnlyList<SpecificationValidationIssue>> HandleAsync(
        ValidateSpecificationDocumentsCommand command, CancellationToken cancellationToken)
    {
        var documents = await documentRepository.ListAsync(command.SpecificationId, cancellationToken);
        var issues = new List<SpecificationValidationIssue>();

        foreach (var group in documents.Where(d => !string.IsNullOrWhiteSpace(d.SpecId)).GroupBy(d => d.SpecId))
        {
            if (group.Count() > 1)
            {
                issues.Add(Issue("duplicate-id",
                    $"Spec ID '{group.Key}' is used by {group.Count()} documents: {string.Join(", ", group.Select(d => d.Path))}.", null));
            }
        }

        var knownIds = documents.Select(d => d.SpecId).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet();

        foreach (var doc in documents)
        {
            foreach (var dep in doc.DependsOn.Where(dep => !knownIds.Contains(dep)))
            {
                issues.Add(Issue("dangling-depends-on", $"'{doc.SpecId}' depends on '{dep}', which doesn't exist.", doc.Path));
            }
        }

        var graph = documents
            .Where(d => !string.IsNullOrWhiteSpace(d.SpecId))
            .ToDictionary(d => d.SpecId, d => d.DependsOn.Where(knownIds.Contains).ToList());
        var visited = new HashSet<string>();
        foreach (var id in graph.Keys)
        {
            if (HasCycle(id, graph, [], visited))
            {
                issues.Add(Issue("dependency-cycle", $"'{id}' is part of a dependency cycle.", null));
            }
        }

        var owningIdsByGeneratedPath = new Dictionary<string, HashSet<string>>();
        foreach (var doc in documents)
        {
            foreach (var path in doc.Generates)
            {
                if (!owningIdsByGeneratedPath.TryGetValue(path, out var owners))
                {
                    owners = [];
                    owningIdsByGeneratedPath[path] = owners;
                }
                owners.Add(doc.SpecId);
            }
        }
        foreach (var (path, owners) in owningIdsByGeneratedPath.Where(kv => kv.Value.Count > 1))
        {
            issues.Add(Issue("path-collision", $"Generated path '{path}' is claimed by multiple specs: {string.Join(", ", owners)}.", null));
        }

        var fullDocuments = await documentRepository.ListAllAsync(command.SpecificationId, cancellationToken);
        foreach (var doc in fullDocuments.Where(d => !d.Content.Contains("## Acceptance criteria", StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(Issue("missing-acceptance-criteria", $"'{doc.SpecId}' has no \"## Acceptance criteria\" section.", doc.Path));
        }

        foreach (var issue in issues)
        {
            await specificationRepository.AddNewChildAsync(issue, cancellationToken);
        }
        if (issues.Count > 0)
        {
            await specificationRepository.SaveChangesAsync(cancellationToken);
        }

        return issues;

        SpecificationValidationIssue Issue(string code, string message, string? path) => new()
        {
            Id = Guid.NewGuid(),
            SpecificationId = command.SpecificationId,
            RunId = command.RunId,
            Severity = ValidationIssueSeverity.Warning,
            Code = code,
            Message = message,
            DocumentPath = path,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Standard three-colour DFS cycle detection over the depends_on graph.</summary>
    private static bool HasCycle(string node, Dictionary<string, List<string>> graph, HashSet<string> visiting, HashSet<string> visited)
    {
        if (visited.Contains(node)) return false;
        if (visiting.Contains(node)) return true;

        visiting.Add(node);
        foreach (var dep in graph.GetValueOrDefault(node, []))
        {
            if (HasCycle(dep, graph, visiting, visited)) return true;
        }
        visiting.Remove(node);
        visited.Add(node);
        return false;
    }
}
