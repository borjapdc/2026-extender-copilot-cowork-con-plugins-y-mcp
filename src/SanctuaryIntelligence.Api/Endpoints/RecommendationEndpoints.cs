using SanctuaryIntelligence.Api.Models;
using SanctuaryIntelligence.Api.Services;

namespace SanctuaryIntelligence.Api.Endpoints;

public static class RecommendationEndpoints
{
    public static void MapRecommendationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recommendations")
            .WithTags("Recommendations");

        group.MapPost("/knight", RecommendKnight)
            .WithName("RecommendKnight")
            .WithSummary("Recommend a knight for a mission")
            .WithDescription("Uses AI to analyze the enemy type, location, urgency, and available knights to recommend the best Saint for the situation, along with risk assessment and strategy.");
    }

    private static async Task<IResult> RecommendKnight(
        KnightRecommendationRequest request,
        RecommendationService service,
        CancellationToken ct)
    {
        var result = await service.RecommendAsync(request, ct);
        return Results.Ok(result);
    }
}
