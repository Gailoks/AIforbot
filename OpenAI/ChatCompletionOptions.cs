namespace TelegramAIBot.OpenAI
{
	internal sealed record class ChatCompletionOptions(
		string ModelName = "gpt-3.5-turbo",
		string? SystemPrompt = null,
		double TopP = 1.0,
		double Temperature = 1.0,
		double FrequencyPenalty = 0.0
	);
}
