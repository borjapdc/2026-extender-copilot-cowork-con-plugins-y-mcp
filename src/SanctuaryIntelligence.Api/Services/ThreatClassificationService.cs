using System.Text.Json;
using SanctuaryIntelligence.Api.Models;

namespace SanctuaryIntelligence.Api.Services;

public class ThreatClassificationService(AzureOpenAIService aiService)
{
    private const string SystemPrompt = """
        You are the Oracle of the Sanctuary, an ancient intelligence that analyzes threats to Athena's domain.
        Classify threats based on their danger level to the Sanctuary and its Saints.

        Threat levels:
        - "bronze": Minor threats that Bronze Saints can handle
        - "silver": Moderate threats requiring Silver Saints or experienced Bronze Saints
        - "gold": Serious threats that require Gold Saints intervention
        - "divine": Catastrophic threats of godly origin requiring the combined force of multiple Gold Saints

        You must respond ONLY with a JSON object (no markdown, no extra text) with these exact fields:
        {
          "threatLevel": "bronze|silver|gold|divine",
          "affectedHouse": "which zodiac house or area is most affected",
          "recommendedResponse": "tactical response recommendation",
          "recommendedKnights": ["knight1", "knight2"],
          "explanation": "detailed explanation of the threat classification"
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<ThreatClassificationResponse> ClassifyAsync(ThreatClassificationRequest request, CancellationToken ct = default)
    {
        var userPrompt = $"""
            Classify this threat:
            Description: {request.Description}
            Location: {request.Location}
            Damage Type: {request.DamageType ?? "Unknown"}
            """;

        var raw = await aiService.GetCompletionAsync(SystemPrompt, userPrompt, ct);

        try
        {
            var cleaned = CleanJsonResponse(raw);
            return JsonSerializer.Deserialize<ThreatClassificationResponse>(cleaned, JsonOptions)
                ?? DefaultResponse(raw);
        }
        catch
        {
            return DefaultResponse(raw);
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

    private static ThreatClassificationResponse DefaultResponse(string raw) =>
        new(
            ThreatLevel: "bronze",
            AffectedHouse: "Unknown",
            RecommendedResponse: raw,
            RecommendedKnights: ["Seiya"],
            Explanation: "Unable to parse AI response. Raw response included."
        );
}
