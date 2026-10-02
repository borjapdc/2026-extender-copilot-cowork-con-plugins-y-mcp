using SanctuaryIntelligence.Api.Models;
using SanctuaryIntelligence.Api.Services;

namespace SanctuaryIntelligence.Api.Endpoints;

public static class BattleEndpoints
{
    public static void MapBattleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/battles")
            .WithTags("Battles");

        group.MapPost("/simulate", Simulate)
            .WithName("SimulateBattle")
            .WithSummary("Simulate a battle")
            .WithDescription("Uses AI to simulate a battle between two knights, analyzing their cosmos levels, techniques, elemental matchups, and battle constraints to predict the outcome.");
    }

    private static async Task<IResult> Simulate(
        BattleSimulationRequest request,
        BattleSimulationService service,
        CancellationToken ct)
    {
        var result = await service.SimulateAsync(request, ct);
        return Results.Ok(result);
    }
}
