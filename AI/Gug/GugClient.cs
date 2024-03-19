using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.AI.Gug
{
	internal sealed class GugClient : IAIClient
	{
		private readonly Configuration _configuration;


		public GugClient(Configuration configuration)
		{
			_configuration = configuration;
		}


		public IChat CreateChat()
		{
			return new GugChat()
			{
				ChatCompletionCreationOperationDuration = _configuration.ChatCompletionCreationOperationDuration
			};
		}


		public class Configuration
		{
			public TimeSpan? ChatCompletionCreationOperationDuration { get; init; } = null;
		}
	}
}
