namespace TelegramAIBot.AI.Abstractions
{
	internal interface IMessageContentVisitor<TResult>
	{
		public TResult VisitImage(ImageMessageContent imageContent);

		public TResult VisitText(TextMessageContent textContent);
	}
}
