namespace TelegramAIBot.AI.Abstractions
{
	internal interface IAIClient
	{
		public IChat CreateChat();

		public Task<TextEmbedding> CreateEmbedding(string model, string text);
	}
}
