namespace SanctuaryIntelligence.Api.Models;

public record MissionGenerationRequest(
    string Objective,
    string Location,
    string[] AssignedKnights,
    string? ThreatLevel
);

public record MissionGenerationResponse(
    string MissionName,
    string Objective,
    string[] AssignedKnights,
    string[] Steps,
    string[] Risks
);
