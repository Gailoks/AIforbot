using System.Collections.Immutable;

namespace TelegramAIBot.AI.Abstractions
{
	internal interface IChat
	{
		public ChatCompletionOptions Options { get; set; }

		public IImmutableList<Message> Messages { get; set; } 


		public void ModifyOptions(Func<ChatCompletionOptions, ChatCompletionOptions> modification);

		public void ModifyMessages(Func<IImmutableList<Message>, IImmutableList<Message>> modification);

		public Task<Message> CreateChatCompletionAsync();
	}
}
