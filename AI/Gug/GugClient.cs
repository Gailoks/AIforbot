using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.AI.Gug
{
	internal sealed class GugClient : IAIClient
	{
		public TimeSpan? ChatCompletionCreationOperationDuration { get; init; } = null;


		public GugClient()
		{

		}


		public IChat CreateChat()
		{
			return new GugChat()
			{
				ChatCompletionCreationOperationDuration = ChatCompletionCreationOperationDuration
			};
		}
	}
}
