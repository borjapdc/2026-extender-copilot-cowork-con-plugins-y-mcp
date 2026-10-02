using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Json;

namespace SanctuaryIntelligence.Mcp.Tools;

[McpServerToolType]
public class ClassifyThreatTool
{
    [McpServerTool(Name = "classify_threat")]
    [Description("Classifies a cosmic threat and proposes a response plan including recommended knights.")]
    public static async Task<string> ClassifyThreat(
        HttpClient httpClient,
        [Description("A description of the threat detected")] string description,
        [Description("The location where the threat was detected (e.g. 'Leo', 'Acuario')")] string location,
        [Description("The type of damage expected (e.g. 'cosmic', 'fire', 'ice', 'psychic')")] string? damageType = null)
    {
        var request = new { description, location, damageType };
        var response = await httpClient.PostAsJsonAsync("/api/threats/classify", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
