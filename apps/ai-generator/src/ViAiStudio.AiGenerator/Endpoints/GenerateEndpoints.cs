using ViAiStudio.AiGenerator.Contracts;
using ViAiStudio.AiGenerator.Providers;

namespace ViAiStudio.AiGenerator.Endpoints;

public static class GenerateEndpoints
{
    public static void MapGenerateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/generate/text", async (GenerateTextRequest request, IModelProvider provider, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await provider.GenerateAsync(
                    new ModelRequest(request.Provider, request.Model, request.BaseUrl, request.ApiKey, request.SystemPrompt, request.Prompt),
                    cancellationToken);

                return Results.Ok(new GenerateTextResponse(result.Text, result.TokensIn, result.TokensOut));
            }
            catch (InvalidOperationException ex)
            {
                // Bad credentials, an unknown deployment name, a malformed base URL,
                // rate limits, etc. -- forward as a clean error instead of an opaque
                // 500 so the Api/wizard can show it to the admin.
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }
        });
    }
}
