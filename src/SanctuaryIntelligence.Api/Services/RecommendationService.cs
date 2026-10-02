using System.Text.Json;
using SanctuaryIntelligence.Api.Data;
using SanctuaryIntelligence.Api.Models;

namespace SanctuaryIntelligence.Api.Services;

public class RecommendationService(AzureOpenAIService aiService)
{
    private static readonly string SystemPrompt = $$"""
        You are the Grand Pope's Strategic Advisor at the Sanctuary.
        Your role is to recommend the best knight for a given mission based on the enemy type,
        location, urgency, and available knights.

        Known knights and their capabilities:
        {{BuildKnightSummary()}}

        Consider elemental matchups, cosmos levels, technique suitability, and urgency when making recommendations.
        Higher urgency means prioritize raw power and speed; lower urgency allows for strategic choices.

        You must respond ONLY with a JSON object (no markdown, no extra text) with these exact fields:
        {
          "recommendedKnight": "knight name",
          "reason": "detailed reasoning for the recommendation",
          "riskLevel": "low|medium|high|extreme",
          "strategy": "recommended battle strategy"
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<KnightRecommendationResponse> RecommendAsync(KnightRecommendationRequest request, CancellationToken ct = default)
    {
        var userPrompt = $"""
            Recommend the best knight for this situation:
            Enemy Type: {request.EnemyType}
            Location: {request.Location}
            Urgency: {request.Urgency}
            Available Knights: {string.Join(", ", request.AvailableKnights)}
            """;

        var raw = await aiService.GetCompletionAsync(SystemPrompt, userPrompt, ct);

        try
        {
            var cleaned = CleanJsonResponse(raw);
            return JsonSerializer.Deserialize<KnightRecommendationResponse>(cleaned, JsonOptions)
                ?? DefaultResponse(request, raw);
        }
        catch
        {
            return DefaultResponse(request, raw);
        }
    }

    private static string CleanJsonResponse(string raw)
    {
        var text = raw.Trim();
        if (text.StartsWith("```"))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
                text = text[start..(end + 1)];
        }
        return text;
    }

    private static KnightRecommendationResponse DefaultResponse(KnightRecommendationRequest request, string raw) =>
        new(
            RecommendedKnight: request.AvailableKnights.FirstOrDefault() ?? "Seiya",
            Reason: raw,
            RiskLevel: "medium",
            Strategy: "Unable to parse AI response. Raw response included."
        );

    private static string BuildKnightSummary() =>
        string.Join("\n", SanctuaryData.GetKnights().Select(k =>
            $"- {k.Name} ({k.Rank}, {k.Constellation}): Cosmos {k.CosmosLevel}, Element: {k.Element}, " +
            $"Techniques: {string.Join(", ", k.Techniques)}"));
}
