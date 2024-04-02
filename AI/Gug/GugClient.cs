using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.AI.Gug
{
	internal sealed class GugClient : IAIClient
	{
		public static readonly EventId ChatCreatedLOG = new EventId(11, nameof(ChatCreatedLOG)).Form();


		private readonly Configuration _configuration;
		private readonly ILogger<GugClient>? _logger;


		public GugClient(IOptions<Configuration> configuration, ILogger<GugClient>? logger = null)
		{
			_configuration = configuration.Value;
			_logger = logger;
		}


		public IChat CreateChat()
		{
			var chatId = Guid.NewGuid();


			_logger?.Log(LogLevel.Information, ChatCreatedLOG, "New chat with id {ChatId} created", chatId);

			return new GugChat(_logger, chatId)
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
