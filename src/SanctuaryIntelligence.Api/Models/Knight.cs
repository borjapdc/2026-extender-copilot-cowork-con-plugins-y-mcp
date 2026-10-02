namespace SanctuaryIntelligence.Api.Models;

public record Knight(
    string Name,
    string Rank, // "Bronze", "Silver", "Gold"
    string Constellation,
    string Element, // "Fire", "Ice", "Lightning", "Earth", "Wind", "Light", "Darkness", "Water", "Psychic"
    string[] Techniques,
    string Armor,
    int CosmosLevel, // 1-100
    string ZodiacHouse, // Zodiac house they guard (only for Gold knights)
    string Description
);

public record KnightRecommendationRequest(
    string EnemyType,
    string Location,
    string Urgency, // "low", "medium", "high", "critical"
    string[] AvailableKnights
);

public record KnightRecommendationResponse(
    string RecommendedKnight,
    string Reason,
    string RiskLevel,
    string Strategy
);
