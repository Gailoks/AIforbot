namespace TelegramAIBot.AI.Abstractions
{
	internal sealed class TextMessageContent : MessageContent
	{
		private readonly string _text;


		public TextMessageContent(string text)
		{
			_text = text;
		}


		public override bool IsPresentableAsString => true;

		public string Text => _text;


		public override string PresentAsString()
		{
			return _text;
		}

		public override TResult Visit<TResult>(IMessageContentVisitor<TResult> visitor)
		{
			return visitor.VisitText(this);
		}
	}
}
