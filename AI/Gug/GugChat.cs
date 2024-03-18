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

			var result = new Message(MessageRole.Assistant, new TextMessageContent(
				$"""
				Answer of Assistant to previous message
				Previous message: {(lastMessage.Content.IsPresentableAsString ? lastMessage.Content.PresentAsString() : "Unable to present as string")}
				Options: {Options}
				"""
			));


			if (ChatCompletionCreationOperationDuration is not null)
				await Task.Delay(ChatCompletionCreationOperationDuration.Value);
			
			return result;
		}
	}
}
