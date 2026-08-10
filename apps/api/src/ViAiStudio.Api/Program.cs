using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ViAiStudio.Api.Endpoints;
using ViAiStudio.Api.Middleware;
using ViAiStudio.Application.Admin;
using ViAiStudio.Application.Auth;
using ViAiStudio.Application.Builds;
using ViAiStudio.Application.Specifications;
using ViAiStudio.Infrastructure;
using ViAiStudio.Infrastructure.Auth;
using ViAiStudio.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var jwtOptions = builder.Configuration.GetSection("Auth:Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            RoleClaimType = JwtClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// Enums (SpecificationStatus, GenerationStatus, AiProvider, AiTaskType) serialize
// as their camelCase names ("building") rather than raw ints, matching what the
// Next.js client's generated TS types expect.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddScoped<CreateSpecificationHandler>();
builder.Services.AddScoped<UpdateSpecificationBasicsHandler>();
builder.Services.AddScoped<SaveSpecificationPhaseHandler>();
builder.Services.AddScoped<GeneratePhaseTextHandler>();
builder.Services.AddScoped<FinalizeSpecificationHandler>();
builder.Services.AddScoped<DeleteSpecificationHandler>();

builder.Services.AddScoped<StartBuildHandler>();
builder.Services.AddScoped<RecordBuildProgressHandler>();
builder.Services.AddScoped<CompleteBuildHandler>();

builder.Services.AddScoped<CreateAiModelConfigHandler>();
builder.Services.AddScoped<UpdateAiModelConfigHandler>();
builder.Services.AddScoped<DeleteAiModelConfigHandler>();
builder.Services.AddScoped<UpdateTaskRoutingHandler>();
builder.Services.AddScoped<GoogleSignInHandler>();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? [builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:3000"];

    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // Browsable endpoint list + try-it-out UI. Schema at /openapi/v1.json, UI at /scalar.
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => Results.Redirect("/scalar"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapAuthEndpoints();
app.MapSpecificationsEndpoints();
app.MapBuildsEndpoints();
app.MapBuildEventsEndpoints();
app.MapAdminEndpoints();
app.MapAuditEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ViAiStudioDbContext>();
    await db.Database.MigrateAsync();
    await AuthSeeder.SeedAsync(db);
}

app.Run();

public partial class Program;
