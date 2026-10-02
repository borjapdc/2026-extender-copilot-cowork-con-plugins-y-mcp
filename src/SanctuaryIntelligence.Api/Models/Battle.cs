namespace SanctuaryIntelligence.Api.Models;

public record BattleSimulationRequest(
    string FighterA,
    string FighterB,
    string Scenario,
    BattleConstraints? Constraints
);

public record BattleConstraints(
    bool NoDivineArmor = false,
    string FatigueLevel = "none" // "none", "low", "medium", "high"
);

public record BattleSimulationResponse(
    Dictionary<string, double> WinnerProbability,
    string Analysis,
    string[] KeyFactors
);
