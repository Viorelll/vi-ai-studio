using ViAiStudio.AiGenerator.Contracts;
using ViAiStudio.AiGenerator.Providers;

namespace ViAiStudio.AiGenerator.Endpoints;

public static class GenerateEndpoints
{
    public static void MapGenerateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/generate/text", async (GenerateTextRequest request, IModelProvider provider, CancellationToken cancellationToken) =>
        {
            var result = await provider.GenerateAsync(
                new ModelRequest(request.Provider, request.Model, request.BaseUrl, request.ApiKey, request.SystemPrompt, request.Prompt),
                cancellationToken);

            return Results.Ok(new GenerateTextResponse(result.Text, result.TokensIn, result.TokensOut));
        });
    }
}
