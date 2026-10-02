using System.Text.Json;
using SanctuaryIntelligence.Api.Models;

namespace SanctuaryIntelligence.Api.Services;

public class MissionGeneratorService(AzureOpenAIService aiService)
{
    private const string SystemPrompt = """
        You are the Sanctuary Tactical Planner, responsible for generating mission plans for Athena's Saints.
        Create detailed, strategic mission plans considering the knights' abilities, the threat level,
        and the location's characteristics.

        You must respond ONLY with a JSON object (no markdown, no extra text) with these exact fields:
        {
          "missionName": "A codename for the mission",
          "objective": "Clear statement of the mission objective",
          "assignedKnights": ["knight1", "knight2"],
          "steps": ["step1", "step2", "step3"],
          "risks": ["risk1", "risk2"]
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<MissionGenerationResponse> GenerateAsync(MissionGenerationRequest request, CancellationToken ct = default)
    {
        var userPrompt = $"""
            Generate a mission plan:
            Objective: {request.Objective}
            Location: {request.Location}
            Assigned Knights: {string.Join(", ", request.AssignedKnights)}
            Threat Level: {request.ThreatLevel ?? "Unknown"}
            """;

        var raw = await aiService.GetCompletionAsync(SystemPrompt, userPrompt, ct);

        try
        {
            var cleaned = CleanJsonResponse(raw);
            return JsonSerializer.Deserialize<MissionGenerationResponse>(cleaned, JsonOptions)
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

    private static MissionGenerationResponse DefaultResponse(MissionGenerationRequest request, string raw) =>
        new(
            MissionName: "Unclassified Mission",
            Objective: request.Objective,
            AssignedKnights: request.AssignedKnights,
            Steps: [raw],
            Risks: ["Unable to parse AI response. Raw response included."]
        );
}
