using SanctuaryIntelligence.Api.Services;

namespace SanctuaryIntelligence.Api.Endpoints;

public static class KnightEndpoints
{
    public static void MapKnightEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/knights")
            .WithTags("Knights");

        group.MapGet("/", GetAll)
            .WithName("GetAllKnights")
            .WithSummary("Get all knights")
            .WithDescription("Returns the complete roster of known Saints in the Sanctuary, including Bronze and Gold knights.");

        group.MapGet("/{name}", GetByName)
            .WithName("GetKnightByName")
            .WithSummary("Get a knight by name")
            .WithDescription("Returns detailed information about a specific knight, searched by name (case-insensitive).");
    }

    private static IResult GetAll(KnightService knightService)
    {
        return Results.Ok(knightService.GetAll());
    }

    private static IResult GetByName(string name, KnightService knightService)
    {
        var knight = knightService.GetByName(name);
        return knight is not null ? Results.Ok(knight) : Results.NotFound(new { message = $"Knight '{name}' not found" });
    }
}
