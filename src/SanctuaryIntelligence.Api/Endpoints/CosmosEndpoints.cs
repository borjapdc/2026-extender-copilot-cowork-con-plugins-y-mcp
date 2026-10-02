using SanctuaryIntelligence.Api.Models;
using SanctuaryIntelligence.Api.Services;

namespace SanctuaryIntelligence.Api.Endpoints;

public static class CosmosEndpoints
{
    public static void MapCosmosEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cosmos")
            .WithTags("Cosmos");

        group.MapPost("/evaluate", Evaluate)
            .WithName("EvaluateCosmos")
            .WithSummary("Evaluate a knight's cosmos energy")
            .WithDescription("Uses AI to evaluate the current cosmos energy level of a knight, considering their baseline power and any contextual factors like fatigue or recent battles.");
    }

    private static async Task<IResult> Evaluate(
        CosmosEvaluationRequest request,
        CosmosEvaluationService service,
        CancellationToken ct)
    {
        var result = await service.EvaluateAsync(request, ct);
        return Results.Ok(result);
    }
}
