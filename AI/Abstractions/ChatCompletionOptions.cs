namespace TelegramAIBot.AI.Abstractions
{
	internal sealed record class ChatCompletionOptions(
		string ModelName = "gpt-3.5-turbo",
		string? SystemPrompt = null,
		double? TopP = null,
		double? Temperature = null,
		double? FrequencyPenalty = null
	);
}
