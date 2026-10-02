using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Web;
using OpenAI;
using SanctuaryIntelligence.Api.Endpoints;
using SanctuaryIntelligence.Api.Services;
using System.ClientModel;
using System.ClientModel.Primitives;

var builder = WebApplication.CreateBuilder(args);

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── OpenAPI / Swagger ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Sanctuary Intelligence API", Version = "v1" });
});

// ── Authentication (optional, controlled by config flag) ──
var authEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled");
if (authEnabled)
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "Authentication");
    var audiences = builder.Configuration.GetSection("Authentication:Audiences").Get<string[]>();
    if (audiences is { Length: > 0 })
    {
        builder.Services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.TokenValidationParameters.ValidAudiences = audiences);
    }

    builder.Services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);
}

// ── Microsoft Foundry / Azure OpenAI via Microsoft.Extensions.AI ──
var foundryEndpoint = builder.Configuration["AzureOpenAI:Endpoint"] ?? "";
var foundryDeployment = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
var foundryApiKey = builder.Configuration["AzureOpenAI:ApiKey"] ?? "";

// Foundry exposes OpenAI-compatible inference through the /openai/v1/ route.
bool isFoundryConfigured = !string.IsNullOrWhiteSpace(foundryEndpoint)
    && !foundryEndpoint.Contains("YOUR-RESOURCE", StringComparison.OrdinalIgnoreCase)
    && Uri.TryCreate(foundryEndpoint, UriKind.Absolute, out _);

if (isFoundryConfigured)
{
    var clientOptions = new OpenAIClientOptions
    {
        Endpoint = GetFoundryOpenAiEndpoint(foundryEndpoint)
    };

#pragma warning disable OPENAI001
    OpenAIClient foundryClient = string.IsNullOrWhiteSpace(foundryApiKey)
        ? new OpenAIClient(
            new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
            clientOptions)
        : new OpenAIClient(new ApiKeyCredential(foundryApiKey), clientOptions);
#pragma warning restore OPENAI001

    builder.Services.AddSingleton<IChatClient>(foundryClient.GetChatClient(foundryDeployment).AsIChatClient());
}
else
{
    // Fallback: mock chat client for development/demo without a Foundry endpoint.
    builder.Services.AddSingleton<IChatClient>(new SanctuaryMockChatClient());
}

// ── Application Services ──
builder.Services.AddSingleton<AzureOpenAIService>();
builder.Services.AddSingleton<KnightService>();
builder.Services.AddSingleton<ThreatClassificationService>();
builder.Services.AddSingleton<BattleSimulationService>();
builder.Services.AddSingleton<MissionGeneratorService>();
builder.Services.AddSingleton<CosmosEvaluationService>();
builder.Services.AddSingleton<RecommendationService>();

var app = builder.Build();

// ── Middleware ──
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// ── Endpoints ──
app.MapGet("/", () => Results.Ok(new
{
    service = "Sanctuary Intelligence API",
    version = "1.0.0",
    status = "operational"
}))
.WithName("Root")
.WithSummary("API health check")
.WithDescription("Returns the API status and version information.")
.ExcludeFromDescription()
.AllowAnonymous();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "sanctuary-intelligence-api"
})).AllowAnonymous();

app.MapKnightEndpoints();
app.MapThreatEndpoints();
app.MapBattleEndpoints();
app.MapMissionEndpoints();
app.MapRecommendationEndpoints();
app.MapCosmosEndpoints();

app.Run();

static Uri GetFoundryOpenAiEndpoint(string endpoint)
{
    var normalizedEndpoint = endpoint.TrimEnd('/');
    var openAiV1Endpoint = normalizedEndpoint.EndsWith("/openai/v1", StringComparison.OrdinalIgnoreCase)
        ? normalizedEndpoint
        : $"{normalizedEndpoint}/openai/v1";

    return new Uri($"{openAiV1Endpoint}/");
}
