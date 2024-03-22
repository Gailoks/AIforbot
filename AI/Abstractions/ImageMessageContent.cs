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

		public override string PresentAsString()
		{
			return _url;
		}

	}
}