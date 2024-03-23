using FluentValidation;
using System.Collections.Immutable;
using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.AI.OpenAI;

namespace TelegramAIBot.AI.Gug
{
	internal sealed class GugChat : AbstractChat
	{
		public TimeSpan? ChatCompletionCreationOperationDuration { get; init; } = null;


		protected override async Task<Message> CreateChatCompletionAsyncInternal()
		{
			var messages = Messages;
			var lastMessage = messages[messages.Count - 1];

			var lastMessageContentsPreview = string.Join('\n',
				lastMessage.Contents.Select(s => $"- `{(s.IsPresentableAsString ? s.PresentAsString() : "Unable to present as string")}`"));

			var result = new Message(MessageRole.Assistant, new TextMessageContent(
				$"""
				Answer of Assistant to previous message
				Previous message:
				{lastMessageContentsPreview}

				Options: `{Options}`
				"""
			));


			if (ChatCompletionCreationOperationDuration is not null)
				await Task.Delay(ChatCompletionCreationOperationDuration.Value);
			
			return result;
		}
	}
}
