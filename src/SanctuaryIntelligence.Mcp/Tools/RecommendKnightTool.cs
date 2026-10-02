using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace SanctuaryIntelligence.Mcp.Tools;

[McpServerToolType]
public class RecommendKnightTool
{
    [McpServerTool(Name = "recommend_knight")]
    [Description("Recommends the most suitable knight to send against a specific threat based on enemy type, location, urgency, and available knights.")]
    public static async Task<string> RecommendKnight(
        HttpClient httpClient,
        [Description("The type of enemy or threat (e.g. 'ice', 'fire', 'psychic', 'darkness')")] string enemyType,
        [Description("The location where the threat was detected (e.g. 'Acuario', 'Leo', 'Sagitario')")] string location,
        [Description("Urgency level: 'low', 'medium', 'high', or 'critical'")] string urgency = "medium",
        [Description("Comma-separated list of available knights (e.g. 'Seiya,Shiryu,Hyoga')")] string? availableKnights = null)
    {
        var knights = availableKnights?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            ?? ["Seiya", "Shiryu", "Hyoga", "Shun", "Ikki"];

        var request = new { enemyType, location, urgency, availableKnights = knights };
        var response = await httpClient.PostAsJsonAsync("/api/recommendations/knight", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
