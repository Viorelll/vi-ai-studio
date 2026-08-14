using System.Text;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Specifications;

/// <summary>
/// Drains one generation run: for each of its ten batches (in order),
/// assembles a prompt from the prompt library + this product's intake +
/// domain interview + what earlier batches already wrote, calls the model
/// once, parses and persists the returned files, re-syncs the MinIO
/// archive, and reports progress over <see cref="IBuildEventBroadcaster"/>
/// (reused unmodified from AI Build, just correlated on the run id instead
/// of a Generation id). Runs entirely inside the Api process -- AI
/// Generator's `/v1/generate/text` is already a synchronous, stateless
/// single call, so ten sequential calls to it is naturally a loop here
/// rather than a second job-queue mechanism inside AI Generator.
/// </summary>
public sealed class RunSpecificationGenerationHandler(
    ISpecificationRepository specificationRepository,
    ISpecificationGenerationRunRepository runRepository,
    ISpecificationDocumentRepository documentRepository,
    ISpecificationPromptLibraryRepository promptLibrary,
    SpecGenerationModelResolver modelResolver,
    IAiCallLogRepository aiCallLogRepository,
    IAiGeneratorClient aiGeneratorClient,
    SyncSpecificationDocumentsHandler syncHandler,
    ValidateSpecificationDocumentsHandler validateHandler,
    RenderSpecificationManifestHandler manifestHandler,
    IBuildEventBroadcaster broadcaster)
{
    private static readonly Dictionary<int, string[]> ExtraTemplatesByBatch = new()
    {
        [3] = ["generation.template.adr"],
        [4] = ["generation.template.entity"],
        [5] = ["generation.template.endpoint"],
        [6] = ["generation.template.screen"],
        [7] = ["generation.template.job", "generation.template.message"],
    };

    public async Task HandleAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await runRepository.GetAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"Generation run '{runId}' does not exist.");
        var specification = await specificationRepository.GetAsync(run.SpecificationId, cancellationToken)
            ?? throw new InvalidOperationException($"Specification '{run.SpecificationId}' does not exist.");
        var intake = specification.Intake
            ?? throw new InvalidOperationException($"Specification '{run.SpecificationId}' has no intake sheet.");

        run.Status = SpecificationGenerationRunStatus.Running;
        run.StartedAt = DateTimeOffset.UtcNow;
        await runRepository.SaveChangesAsync(cancellationToken);

        var systemPromptTemplate = await promptLibrary.GetAsync("generation.system-prompt", cancellationToken);
        var systemPrompt = systemPromptTemplate?.Content ?? "You write software specifications as JSON.";

        var config = await modelResolver.ResolveAsync(cancellationToken);
        var credentials = ModelCredentials.FromConfig(config);

        var plans = SpecificationGenerationBatchPlanner.Plan(intake).ToDictionary(p => p.BatchIndex);
        var totalBatches = run.Batches.Count;
        var allocatedIds = new List<string>();
        var failed = false;

        foreach (var batch in run.Batches.OrderBy(b => b.BatchIndex))
        {
            if (batch.Status == SpecificationGenerationBatchStatus.Skipped)
            {
                Publish(runId, batch, totalBatches, $"Skipped {batch.Name} -- {batch.Note}");
                continue;
            }

            batch.Status = SpecificationGenerationBatchStatus.Running;
            batch.StartedAt = DateTimeOffset.UtcNow;
            await runRepository.SaveChangesAsync(cancellationToken);
            Publish(runId, batch, totalBatches, $"Starting {batch.Name}…");

            try
            {
                var priorDocuments = await documentRepository.ListAsync(specification.Id, cancellationToken);
                var plan = plans[batch.BatchIndex];
                var prompt = await BuildPromptAsync(specification, plan, priorDocuments, allocatedIds, cancellationToken);

                var generated = await aiGeneratorClient.GenerateTextAsync(credentials, systemPrompt, prompt, cancellationToken);

                await aiCallLogRepository.AddAsync(new AiCallLog
                {
                    Id = Guid.NewGuid(),
                    SpecificationId = specification.Id,
                    GenerationVersion = null,
                    Task = AiTaskType.SpecGeneration,
                    Model = config.Label,
                    TokensIn = generated.TokensIn,
                    TokensOut = generated.TokensOut,
                    Prompt = prompt,
                    Result = generated.Text,
                    CreatedAt = DateTimeOffset.UtcNow,
                }, cancellationToken);
                await aiCallLogRepository.SaveChangesAsync(cancellationToken);

                var parsed = SpecificationBatchResponseParser.Parse(generated.Text);
                foreach (var parseError in parsed.Errors)
                {
                    await AddValidationIssueAsync(specification.Id, run.Id, "batch-parse-error", $"{batch.Name}: {parseError}", null, cancellationToken);
                }

                var writtenIds = new List<string>();
                foreach (var file in parsed.Files)
                {
                    var document = SpecificationDocumentRenderer.Render(specification.Id, batch.Id, file);
                    await documentRepository.UpsertAsync(document, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(file.SpecId))
                    {
                        writtenIds.Add(file.SpecId);
                    }
                }
                await documentRepository.SaveChangesAsync(cancellationToken);
                allocatedIds.AddRange(writtenIds);

                batch.AllocatedIds = writtenIds;
                batch.FilesWritten = parsed.Files.Count;
                batch.Status = SpecificationGenerationBatchStatus.Ready;
                batch.CompletedAt = DateTimeOffset.UtcNow;
                await runRepository.SaveChangesAsync(cancellationToken);

                await syncHandler.HandleAsync(specification, cancellationToken);
                Publish(runId, batch, totalBatches, $"Finished {batch.Name} -- {parsed.Files.Count} file(s)");
            }
            catch (Exception ex)
            {
                batch.Status = SpecificationGenerationBatchStatus.Failed;
                batch.Note = ex.Message;
                batch.CompletedAt = DateTimeOffset.UtcNow;
                await runRepository.SaveChangesAsync(cancellationToken);
                Publish(runId, batch, totalBatches, $"Failed {batch.Name}: {ex.Message}");
                failed = true;
                break;
            }
        }

        if (!failed)
        {
            await validateHandler.HandleAsync(new ValidateSpecificationDocumentsCommand(specification.Id, run.Id), cancellationToken);
            await manifestHandler.HandleAsync(specification, cancellationToken);
            await syncHandler.HandleAsync(specification, cancellationToken);
        }

        run.Status = failed ? SpecificationGenerationRunStatus.Failed : SpecificationGenerationRunStatus.Ready;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.DurationSeconds = run.StartedAt.HasValue ? (int)(run.CompletedAt.Value - run.StartedAt.Value).TotalSeconds : null;
        await runRepository.SaveChangesAsync(cancellationToken);

        specification.Status = failed ? SpecificationStatus.Failed : SpecificationStatus.Ready;
        await specificationRepository.SaveChangesAsync(cancellationToken);

        broadcaster.Publish(new BuildEvent(
            runId, failed ? "Failed" : "Done", failed ? "Generation failed." : "Specification generated.", 100, Done: true));
    }

    private async Task<string> BuildPromptAsync(
        Specification specification, BatchPlan plan, IReadOnlyList<SpecificationDocumentSummary> priorDocuments,
        IReadOnlyList<string> allocatedIds, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        async Task AppendTemplateAsync(string key)
        {
            var template = await promptLibrary.GetAsync(key, cancellationToken);
            if (template is not null)
            {
                sb.AppendLine(template.Content).AppendLine();
            }
        }

        await AppendTemplateAsync("generation.authoring-rules");
        await AppendTemplateAsync("generation.id-scheme");
        await AppendTemplateAsync("generation.output-shape");
        await AppendTemplateAsync("generation.file-rules");
        await AppendTemplateAsync("generation.consistency-rules");
        await AppendTemplateAsync("generation.template.spec");
        foreach (var key in ExtraTemplatesByBatch.GetValueOrDefault(plan.BatchIndex, []))
        {
            await AppendTemplateAsync(key);
        }

        var batchTemplate = await promptLibrary.GetAsync($"generation.batch.{plan.BatchIndex}", cancellationToken);
        var batchInstructions = batchTemplate?.Content ?? string.Empty;
        foreach (var (key, value) in plan.Placeholders)
        {
            batchInstructions = batchInstructions.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
        }
        sb.AppendLine(batchInstructions).AppendLine();

        var stack = specification.Stack;
        sb.AppendLine("Technology stack (use exactly these -- do not substitute):");
        sb.Append("- Backend: ").AppendLine(stack.Backend);
        sb.Append("- UI framework: ").AppendLine(stack.Ui);
        sb.Append("- Database: ").AppendLine(stack.Database);
        sb.Append("- Containerization: ").AppendLine(stack.Infra);
        sb.Append("- UI style: ").AppendLine(stack.UiStyle);
        sb.AppendLine();

        sb.AppendLine(SpecificationIntakeRenderer.Render(specification));
        sb.AppendLine();

        if (priorDocuments.Count > 0)
        {
            sb.AppendLine("=== DOCUMENTS ALREADY WRITTEN (path | id | title | depends_on) ===");
            foreach (var doc in priorDocuments)
            {
                sb.Append(doc.Path).Append(" | ").Append(doc.SpecId).Append(" | ").Append(doc.Title)
                    .Append(" | depends_on: [").Append(string.Join(", ", doc.DependsOn)).AppendLine("]");
            }
            sb.AppendLine();
        }

        if (allocatedIds.Count > 0)
        {
            sb.AppendLine("=== IDs ALREADY ALLOCATED (do not reuse) ===");
            sb.AppendLine(string.Join(", ", allocatedIds));
            sb.AppendLine();
        }

        var responseFormat = await promptLibrary.GetAsync("generation.response-format", cancellationToken);
        if (responseFormat is not null)
        {
            sb.AppendLine(responseFormat.Content);
        }

        return sb.ToString();
    }

    private async Task AddValidationIssueAsync(
        Guid specificationId, Guid runId, string code, string message, string? documentPath, CancellationToken cancellationToken)
    {
        var issue = new SpecificationValidationIssue
        {
            Id = Guid.NewGuid(),
            SpecificationId = specificationId,
            RunId = runId,
            Severity = ValidationIssueSeverity.Warning,
            Code = code,
            Message = message,
            DocumentPath = documentPath,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await specificationRepository.AddNewChildAsync(issue, cancellationToken);
        await specificationRepository.SaveChangesAsync(cancellationToken);
    }

    private void Publish(Guid runId, SpecificationGenerationBatch batch, int totalBatches, string logLine)
    {
        var progressPct = totalBatches == 0 ? 100 : (int)Math.Round(batch.BatchIndex / (double)totalBatches * 100);
        broadcaster.Publish(new BuildEvent(runId, batch.Name, logLine, progressPct, Done: false));
    }
}
