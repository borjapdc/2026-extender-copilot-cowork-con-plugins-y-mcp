using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Json;

namespace SanctuaryIntelligence.Mcp.Tools;

[McpServerToolType]
public class EvaluateCosmosTool
{
    [McpServerTool(Name = "evaluate_cosmos")]
    [Description("Evaluates the cosmos energy level of a knight.")]
    public static async Task<string> EvaluateCosmos(
        HttpClient httpClient,
        [Description("The name of the knight to evaluate (e.g. 'Shiryu')")] string knightName,
        [Description("Optional context for the evaluation (e.g. 'after a long battle')")] string? context = null)
    {
        var request = new { knightName, context };
        var response = await httpClient.PostAsJsonAsync("/api/cosmos/evaluate", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
