namespace TelegramAIBot.OpenAI
{
	internal sealed class TextMessageContent : MessageContent
	{
		private readonly string _text;


		public TextMessageContent(string text)
		{
			_text = text;
		}


		public override bool IsPresentableAsString => true;


		public override string PresentAsString()
		{
			return _text;
		}
	}
}
