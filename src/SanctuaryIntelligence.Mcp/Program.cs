using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Authentication (Demo 2: flip Authentication:Enabled to true) ──
var authEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled");
if (authEnabled)
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "Authentication");
    builder.Services.AddAuthorization();

    // SSO via Microsoft Entra: aceptar todas las audiencias configuradas
    // (el token store de Microsoft usa el App ID URI generado por el Dev Portal).
    var audiences = builder.Configuration.GetSection("Authentication:Audiences").Get<string[]>();
    if (audiences is { Length: > 0 })
    {
        builder.Services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.TokenValidationParameters.ValidAudiences = audiences;
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenInvalidAudienceException exception)
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("SanctuaryIntelligence.Mcp.Authentication");
                            logger.LogWarning(
                                "Rejected token audience {ReceivedAudience}. Allowed audiences: {AllowedAudiences}",
                                exception.InvalidAudience,
                                string.Join(", ", audiences));
                        }

                        return Task.CompletedTask;
                    }
                };
            });
    }
}

// Configure HttpClient for the Sanctuary API
builder.Services.AddHttpClient("SanctuaryApi", client =>
{
    var apiUrl = builder.Configuration["SanctuaryApi:BaseUrl"] ?? "http://localhost:5100";
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register HttpClient for DI into MCP tools
builder.Services.AddTransient(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("SanctuaryApi"));

// Add MCP server with HTTP transport and all tools
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "Sanctuary Intelligence MCP Server",
            Version = "1.0.0"
        };
    })
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithTools<SanctuaryIntelligence.Mcp.Tools.RecommendKnightTool>()
    .WithTools<SanctuaryIntelligence.Mcp.Tools.SimulateBattleTool>()
    .WithTools<SanctuaryIntelligence.Mcp.Tools.ClassifyThreatTool>()
    .WithTools<SanctuaryIntelligence.Mcp.Tools.GenerateMissionTool>()
    .WithTools<SanctuaryIntelligence.Mcp.Tools.EvaluateCosmosTool>();

// CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    service = "sanctuary-intelligence-mcp-server",
    authentication = authEnabled ? "Entra ID (Bearer JWT)" : "None (demo mode)"
}));

// Tools list endpoint for debugging
app.MapGet("/tools", () => Results.Ok(new
{
    tools = new[]
    {
        new { name = "recommend_knight", description = "Recommends the most suitable knight for a threat" },
        new { name = "simulate_battle", description = "Simulates a battle between two fighters" },
        new { name = "classify_threat", description = "Classifies a cosmic threat and proposes response" },
        new { name = "generate_mission", description = "Generates a tactical mission plan" },
        new { name = "evaluate_cosmos", description = "Evaluates a knight's cosmos energy level" }
    }
}));

// Demo 2: con auth → el endpoint MCP exige Bearer token de Entra ID
// Demo 1: sin auth → acceso anónimo
if (authEnabled)
    app.MapMcp("/mcp").RequireAuthorization();
else
    app.MapMcp("/mcp");

app.Run();
