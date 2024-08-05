using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.Telemetry;

namespace TelegramAIBot.User;

class Session(IAIChat chat)
{
    private readonly IAIChat _chat = chat;

    public async Task<string> AskAsync(string question)
    {
        _chat.Messages.Add(new(MessageRole.User, question));
        return (await _chat.CreateChatCompletionAsync()).Content;
    }
    public string? SystemPrompt
    {
        get => _chat.Options.SystemPrompt;
        set => _chat.ModifyOptions(options => options with { SystemPrompt = value });
    }

    public Task WriteTelemetryAsync(ITelemetryStorage storage, long userId)
    {
        var data = new Dictionary<string, object?>()
        {
            ["messages"] = _chat.Messages.Select(s => new { role = s.Role, content = s.Content }).ToArray(),
            ["system"] = _chat.Options.SystemPrompt
        };
        return storage.CreateEntryAsync(userId.ToString(), new(data));
    }
}