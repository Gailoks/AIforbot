namespace TelegramAIBot.AI.Abstractions
{
	internal record class Message(MessageRole Role, IReadOnlyList<MessageContent> Contents)
	{
		public Message(MessageRole role, MessageContent content) : this(role, [content]) { }


		public MessageContent Content => Contents.Single();
	}
}
