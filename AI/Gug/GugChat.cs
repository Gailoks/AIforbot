using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.AI.Gug
{
	internal sealed class GugChat : AbstractChat
	{
		public TimeSpan? ChatCompletionCreationOperationDuration { get; init; } = null;


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
			
			return result;
		}
	}
}
