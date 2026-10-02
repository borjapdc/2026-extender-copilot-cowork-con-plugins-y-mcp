namespace SanctuaryIntelligence.Api.Models;

public record ThreatClassificationRequest(
    string Description,
    string Location,
    string? DamageType
);

public record ThreatClassificationResponse(
    string ThreatLevel, // "bronze", "silver", "gold", "divine"
    string AffectedHouse,
    string RecommendedResponse,
    string[] RecommendedKnights,
    string Explanation
);
