using System.Text.Json;
using SanctuaryIntelligence.Api.Models;

namespace SanctuaryIntelligence.Api.Services;

public class CosmosEvaluationService(AzureOpenAIService aiService)
{
    private const string SystemPrompt = """
        You are the Sanctuary Cosmos Analyzer, an expert in measuring and evaluating the cosmos energy of Saints.
        Cosmos is the fundamental energy that Saints use to perform their techniques.

        Evaluate the current cosmos state of a knight considering their known baseline,
        recent activities, and any contextual factors.

        Status levels:
        - "peak": Cosmos is at maximum potential, all techniques available at full power
        - "stable": Normal operational level, reliable combat performance
        - "unstable but operational": Fluctuating cosmos, may have difficulty with advanced techniques
        - "critical": Dangerously low cosmos, basic techniques only
        - "depleted": No combat capability, requires rest and recovery

        You must respond ONLY with a JSON object (no markdown, no extra text) with these exact fields:
        {
          "cosmosScore": 85,
          "status": "stable",
          "recommendation": "tactical recommendation for the knight",
          "confidence": 0.9
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<CosmosEvaluationResponse> EvaluateAsync(CosmosEvaluationRequest request, CancellationToken ct = default)
    {
        var userPrompt = $"""
            Evaluate the cosmos energy of: {request.KnightName}
            Context: {request.Context ?? "Standard evaluation, no special conditions"}
            """;

        var raw = await aiService.GetCompletionAsync(SystemPrompt, userPrompt, ct);

        try
        {
            var cleaned = CleanJsonResponse(raw);
            return JsonSerializer.Deserialize<CosmosEvaluationResponse>(cleaned, JsonOptions)
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

    private static CosmosEvaluationResponse DefaultResponse(string raw) =>
        new(
            CosmosScore: 50,
            Status: "stable",
            Recommendation: raw,
            Confidence: 0.0
        );
}
