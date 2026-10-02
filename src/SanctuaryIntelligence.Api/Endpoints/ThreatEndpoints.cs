using SanctuaryIntelligence.Api.Models;
using SanctuaryIntelligence.Api.Services;

namespace SanctuaryIntelligence.Api.Endpoints;

public static class ThreatEndpoints
{
    public static void MapThreatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/threats")
            .WithTags("Threats");

        group.MapPost("/classify", Classify)
            .WithName("ClassifyThreat")
            .WithSummary("Classify a threat")
            .WithDescription("Uses AI to classify a threat's danger level, determine the affected zodiac house, and recommend a response strategy with suitable knights.");
    }

    private static async Task<IResult> Classify(
        ThreatClassificationRequest request,
        ThreatClassificationService service,
        CancellationToken ct)
    {
        var result = await service.ClassifyAsync(request, ct);
        return Results.Ok(result);
    }
}
