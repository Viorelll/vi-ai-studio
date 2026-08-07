namespace ViAiStudio.AiGenerator.Contracts;

public sealed record GenerateTextRequest(string Provider, string Model, string BaseUrl, string ApiKey, string SystemPrompt, string Prompt);

public sealed record GenerateTextResponse(string Text, int TokensIn, int TokensOut);
