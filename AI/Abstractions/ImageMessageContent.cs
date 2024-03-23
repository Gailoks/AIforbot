namespace TelegramAIBot.AI.Abstractions
{
	internal sealed class ImageMessageContent : MessageContent
	{
		private readonly string _url;


		public ImageMessageContent(string url)
		{
			_url = url;
		}


		public override bool IsPresentableAsString => true;

		public string Url => _url;


		public override string PresentAsString()
		{
			return _url;
		}

		public override TResult Visit<TResult>(IMessageContentVisitor<TResult> visitor)
		{
			return visitor.VisitImage(this);
		}
	}
}