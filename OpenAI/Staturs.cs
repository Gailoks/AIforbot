namespace TelegramAIBot.OpenAI
{
    internal enum Status
    {
        None,
        Working,
        ChangingTemp,
        ChangingTopP,
        ChangingSystemPrompt
    }
}