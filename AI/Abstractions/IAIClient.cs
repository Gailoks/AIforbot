namespace TelegramAIBot.AI.Abstractions
{
	internal interface IAIClient
	{
		public IChat CreateChat();

		public Task<TextEmbedding> CreateEmbeddingAsync(string model, string text);
	}
}
