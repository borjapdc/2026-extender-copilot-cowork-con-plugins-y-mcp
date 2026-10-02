using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Json;

namespace SanctuaryIntelligence.Mcp.Tools;

[McpServerToolType]
public class GenerateMissionTool
{
    [McpServerTool(Name = "generate_mission")]
    [Description("Generates a tactical mission to protect a zodiac house.")]
    public static async Task<string> GenerateMission(
        HttpClient httpClient,
        [Description("The mission objective (e.g. 'Protect the passage to Athena\\'s chamber')")] string objective,
        [Description("The location of the mission (e.g. 'Sagitario', 'Leo')")] string location,
        [Description("Comma-separated list of knights assigned to the mission (e.g. 'Seiya,Shiryu,Shun')")] string assignedKnights,
        [Description("The threat level: 'bronze', 'silver', 'gold', or 'divine'")] string? threatLevel = null)
    {
        var knights = assignedKnights.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var request = new { objective, location, assignedKnights = knights, threatLevel };
        var response = await httpClient.PostAsJsonAsync("/api/missions/generate", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
