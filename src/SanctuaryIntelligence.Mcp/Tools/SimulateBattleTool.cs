using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Json;

namespace SanctuaryIntelligence.Mcp.Tools;

[McpServerToolType]
public class SimulateBattleTool
{
    [McpServerTool(Name = "simulate_battle")]
    [Description("Simulates a battle between two knights or enemies and predicts the outcome.")]
    public static async Task<string> SimulateBattle(
        HttpClient httpClient,
        [Description("The first fighter (e.g. 'Seiya')")] string fighterA,
        [Description("The second fighter (e.g. 'Ikki')")] string fighterB,
        [Description("The battle scenario or location (e.g. 'Sanctuary', 'Colosseum')")] string scenario = "Sanctuary",
        [Description("Whether divine armor (cloth) is removed from both fighters")] bool noDivineArmor = false,
        [Description("Fatigue level: 'none', 'low', 'medium', 'high', or 'extreme'")] string fatigueLevel = "none")
    {
        var request = new { fighterA, fighterB, scenario, noDivineArmor, fatigueLevel };
        var response = await httpClient.PostAsJsonAsync("/api/battles/simulate", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
