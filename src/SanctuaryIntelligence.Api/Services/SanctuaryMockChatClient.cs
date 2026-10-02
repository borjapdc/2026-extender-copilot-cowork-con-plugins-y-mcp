using Microsoft.Extensions.AI;

namespace SanctuaryIntelligence.Api.Services;

/// <summary>
/// Fallback IChatClient implementation for demos/CI without Azure OpenAI credentials.
/// Returns realistic Saint Seiya-themed JSON responses.
/// </summary>
public sealed class SanctuaryMockChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var userMsg = messages
            .LastOrDefault(m => m.Role == ChatRole.User)
            ?.Text ?? string.Empty;

        var text = GenerateMockResponse(userMsg);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Streaming is not supported in mock mode.");

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }

    private static string GenerateMockResponse(string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();

        // RecommendationService prompt contains "Available Knights:" (unique to this service)
        if (lower.Contains("available knights:"))
        {
            return """
                {
                  "recommendedKnight": "Seiya",
                  "reason": "[MOCK] Seiya of Pegasus possesses unmatched burning cosmos and the ability to break divine barriers. His raw willpower transcends all limitations.",
                  "riskLevel": "medium",
                  "strategy": "Deploy Seiya as primary attacker using Pegasus Meteor Fist. Support knights protect flanks and provide backup cosmos energy."
                }
                """;
        }

        // BattleSimulationService prompt contains "Fighter A:" (unique to this service)
        // Expected fields: winnerProbability (dict), analysis, keyFactors (array)
        if (lower.Contains("fighter a:"))
        {
            return """
                {
                  "winnerProbability": { "Seiya": 0.62, "Opponent": 0.38 },
                  "analysis": "[MOCK] Seiya's burning cosmos and relentless spirit give him a decisive edge. His 7th Sense activation in critical moments shifts the balance.",
                  "keyFactors": [
                    "Seiya's cosmos level peaks under pressure",
                    "Pegasus Meteor Fist overwhelms opponent defenses",
                    "[MOCK] Simulated without real AI — configure AzureOpenAI:Endpoint for live analysis"
                  ]
                }
                """;
        }

        // ThreatClassificationService prompt contains "Damage Type:" (unique to this service)
        // Expected fields: threatLevel, affectedHouse, recommendedResponse, recommendedKnights (array), explanation
        if (lower.Contains("damage type:"))
        {
            return """
                {
                  "threatLevel": "gold",
                  "affectedHouse": "House of Gemini",
                  "recommendedResponse": "[MOCK] Dispatch Gold Saints immediately. Coordinate defensive perimeter with Bronze Saint support units.",
                  "recommendedKnights": ["Seiya", "Shiryu", "Hyoga", "Shun", "Ikki"],
                  "explanation": "[MOCK] Gold-level threat detected. Cosmic energy readings exceed Bronze and Silver Saint classification thresholds. Immediate Gold Saint response required."
                }
                """;
        }

        // MissionGeneratorService prompt contains "Assigned Knights:" (unique to this service)
        // Expected fields: missionName, objective, assignedKnights (array), steps (array), risks (array)
        if (lower.Contains("assigned knights:"))
        {
            return """
                {
                  "missionName": "Operation Shield of Athena",
                  "objective": "Neutralize the divine threat and protect the Sanctuary perimeter",
                  "assignedKnights": ["Seiya", "Shiryu", "Hyoga"],
                  "steps": [
                    "Phase 1: Establish defensive perimeter around the affected Zodiac House",
                    "Phase 2: Seiya leads frontal engagement while Shiryu contains collateral damage",
                    "Phase 3: Hyoga secures retreat path and provides elemental support",
                    "[MOCK] Full tactical plan — configure AzureOpenAI:Endpoint for AI-generated missions"
                  ],
                  "risks": [
                    "[MOCK] Simulated risk assessment — cosmos drain during extended engagement",
                    "Potential for divine-level escalation if primary objective fails"
                  ]
                }
                """;
        }

        // CosmosEvaluationService prompt contains "cosmos energy of:" (unique to this service)
        // Expected fields: cosmosScore (int), status, recommendation, confidence (double)
        if (lower.Contains("cosmos energy of:"))
        {
            return """
                {
                  "cosmosScore": 82,
                  "status": "stable",
                  "recommendation": "[MOCK] Knight is operating at stable cosmos levels. Recommend standard training regimen to approach peak performance. Full 7th Sense access available.",
                  "confidence": 0.85
                }
                """;
        }

        return "[MOCK] The Grand Pope's Oracle has received your request. Configure AzureOpenAI:Endpoint in user-secrets for real AI responses.";
    }
}
