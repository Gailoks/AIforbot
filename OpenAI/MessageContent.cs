namespace TelegramAIBot.OpenAI
{
    internal abstract class MessageContent
    {
		public abstract bool IsPresentableAsString { get; }

		public virtual string PresentAsString() => throw new NotSupportedException();
    }
}
