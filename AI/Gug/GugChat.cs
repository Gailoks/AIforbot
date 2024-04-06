using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.AI.Gug
{
	internal sealed class GugChat : AbstractChat
	{
		public static readonly EventId ChatCompletionCreatedLOG = new EventId(31, nameof(ChatCompletionCreatedLOG)).Form();
		public static readonly EventId ChatMessagesChangedLOG = new EventId(32, nameof(ChatMessagesChangedLOG)).Form();
		public static readonly EventId ChatOptionsChangedLOG = new EventId(33, nameof(ChatOptionsChangedLOG)).Form();


		private readonly ILogger<GugClient>? _logger;


		public TimeSpan? ChatCompletionCreationOperationDuration { get; init; } = null;


		public GugChat(ILogger<GugClient>? logger, Guid id) : base(id)
		{
			_logger = logger;
			MessagesChanged += (old) => _logger?.Log(LogLevel.Debug, ChatMessagesChangedLOG, "Chat {ChatId} messages changed", id);
			OptionsChanged += (old) => _logger?.Log(LogLevel.Debug, ChatOptionsChangedLOG, "Chat {ChatId} options changed to {Options}", id, Options);
		}


		protected override async Task<Message> CreateChatCompletionAsyncInternal()
		{
			var messages = Messages;
			var lastMessageContent = messages[messages.Count - 1].Content;

			var result = new Message(MessageRole.Assistant, new TextMessageContent(
				$"""
				Answer of Assistant to previous message
				Previous message:
				`{(lastMessageContent.IsPresentableAsString ? lastMessageContent.PresentAsString() : "Unable to present as string")}`

				Options: `{Options}`
				"""
			));


			if (ChatCompletionCreationOperationDuration is not null)
				await Task.Delay(ChatCompletionCreationOperationDuration.Value);

			_logger?.Log(LogLevel.Debug, ChatCompletionCreatedLOG,
				"Chat completion created with Options {Options}. Previous message had content {Content}",
				Options, lastMessageContent);
			
			return result;
		}
	}
}
