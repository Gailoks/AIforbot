using FluentValidation;

namespace TelegramAIBot.OpenAI
{
	internal sealed class ChatCompletionOptionsValidator : AbstractValidator<ChatCompletionOptions>
	{
		public ChatCompletionOptionsValidator()
		{
			RuleFor(s => s.Temperature).InclusiveBetween(0.0, 2.0);
			RuleFor(s => s.TopP).InclusiveBetween(0.0, 1.0);
			RuleFor(s => s.FrequencyPenalty).InclusiveBetween(-2.0, 2.0);
		}
	}
}
