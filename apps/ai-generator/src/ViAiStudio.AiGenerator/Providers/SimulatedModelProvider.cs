namespace ViAiStudio.AiGenerator.Providers;

/// <summary>
/// Placeholder <see cref="IModelProvider"/> that fabricates a plausible reply
/// instead of calling a real provider endpoint. Swap in an OpenAiProvider /
/// AnthropicProvider / etc. behind the same interface once real credentials
/// and provider SDKs are wired up -- every caller already goes through
/// <see cref="IModelProvider"/>, so nothing else has to change.
/// </summary>
public sealed class SimulatedModelProvider : IModelProvider
{
    public Task<ModelResult> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        var text = $"[{request.Model}] {Summarize(request.Prompt)}";
        var tokensIn = EstimateTokens(request.SystemPrompt) + EstimateTokens(request.Prompt);
        var tokensOut = EstimateTokens(text);
        return Task.FromResult(new ModelResult(text, tokensIn, tokensOut));
    }

    private static string Summarize(string prompt)
    {
        var firstLine = prompt.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? prompt;
        return $"Drafted a response for: {firstLine.Trim()}";
    }

    private static int EstimateTokens(string text) =>
        Math.Max(1, text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length * 4 / 3);
}
