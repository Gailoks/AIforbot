namespace TelegramAIBot.AI.Abstractions
{
	internal interface IMessageContentVisitor<TResult>
	{
		public TResult VisitText(TextMessageContent textContent);
	}
}
