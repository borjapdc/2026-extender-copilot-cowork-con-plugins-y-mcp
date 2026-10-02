using System.Text.Json;
using SanctuaryIntelligence.Api.Data;
using SanctuaryIntelligence.Api.Models;

namespace SanctuaryIntelligence.Api.Services;

public class BattleSimulationService(AzureOpenAIService aiService)
{
    private static readonly string SystemPrompt = $$"""
        You are the Sanctuary Battle Simulator, an advanced system that predicts combat outcomes between Saints.
        You have deep knowledge of each knight's techniques, cosmos level, and combat style.

        Known knights and their data:
        {{BuildKnightSummary()}}

        Analyze the matchup considering: cosmos levels, elemental advantages, technique compatibility,
        battle experience, and any specified constraints.

        You must respond ONLY with a JSON object (no markdown, no extra text) with these exact fields:
        {
          "winnerProbability": { "FighterA_Name": 0.65, "FighterB_Name": 0.35 },
          "analysis": "detailed analysis of the battle",
          "keyFactors": ["factor1", "factor2", "factor3"]
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<BattleSimulationResponse> SimulateAsync(BattleSimulationRequest request, CancellationToken ct = default)
    {
        var constraints = request.Constraints ?? new BattleConstraints();
        var userPrompt = $"""
            Simulate a battle between:
            Fighter A: {request.FighterA}
            Fighter B: {request.FighterB}
            Scenario: {request.Scenario}
            Constraints: No Divine Armor = {constraints.NoDivineArmor}, Fatigue Level = {constraints.FatigueLevel}
            """;

        var raw = await aiService.GetCompletionAsync(SystemPrompt, userPrompt, ct);

        try
        {
            var cleaned = CleanJsonResponse(raw);
            return JsonSerializer.Deserialize<BattleSimulationResponse>(cleaned, JsonOptions)
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

    private static BattleSimulationResponse DefaultResponse(BattleSimulationRequest request, string raw) =>
        new(
            WinnerProbability: new Dictionary<string, double>
            {
                [request.FighterA] = 0.5,
                [request.FighterB] = 0.5
            },
            Analysis: raw,
            KeyFactors: ["Unable to parse AI response. Raw analysis included."]
        );

    private static string BuildKnightSummary() =>
        string.Join("\n", SanctuaryData.GetKnights().Select(k =>
            $"- {k.Name} ({k.Rank}, {k.Constellation}): Cosmos {k.CosmosLevel}, Element: {k.Element}, " +
            $"Techniques: {string.Join(", ", k.Techniques)}"));
}
