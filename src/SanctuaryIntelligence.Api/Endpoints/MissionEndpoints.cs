using SanctuaryIntelligence.Api.Models;
using SanctuaryIntelligence.Api.Services;

namespace SanctuaryIntelligence.Api.Endpoints;

public static class MissionEndpoints
{
    public static void MapMissionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/missions")
            .WithTags("Missions");

        group.MapPost("/generate", Generate)
            .WithName("GenerateMission")
            .WithSummary("Generate a mission plan")
            .WithDescription("Uses AI to generate a detailed tactical mission plan, including steps, assigned knights, and risk assessment.");
    }

    private static async Task<IResult> Generate(
        MissionGenerationRequest request,
        MissionGeneratorService service,
        CancellationToken ct)
    {
        var result = await service.GenerateAsync(request, ct);
        return Results.Ok(result);
    }
}
