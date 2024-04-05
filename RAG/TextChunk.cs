using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.RAG
{
	internal readonly struct TextChunk
	{
		public TextChunk(string text, TextEmbedding embedding) : this()
		{
			Text = text;
			Embedding = embedding;
		}


		public string Text { get; }

		public TextEmbedding Embedding { get; }
	}
}