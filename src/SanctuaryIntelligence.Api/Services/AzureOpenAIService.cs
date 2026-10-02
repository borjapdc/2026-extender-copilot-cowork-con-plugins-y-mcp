namespace SanctuaryIntelligence.Api.Services;

using Microsoft.Extensions.AI;

public class AzureOpenAIService(IChatClient chatClient)
{
    public async Task<string> GetCompletionAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text ?? string.Empty;
    }
}
