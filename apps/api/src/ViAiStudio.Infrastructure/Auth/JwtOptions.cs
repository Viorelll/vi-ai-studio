namespace ViAiStudio.Infrastructure.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "ViAiStudio";
    public string Audience { get; set; } = "ViAiStudio.Web";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}