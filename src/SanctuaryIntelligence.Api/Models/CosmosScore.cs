namespace SanctuaryIntelligence.Api.Models;

public record CosmosEvaluationRequest(
    string KnightName,
    string? Context // e.g. "after a long battle", "during training"
);

public record CosmosEvaluationResponse(
    int CosmosScore,
    string Status, // "peak", "stable", "unstable but operational", "critical", "depleted"
    string Recommendation,
    double Confidence
);
