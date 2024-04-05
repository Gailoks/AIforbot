using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.RAG
{
	internal sealed class RAGProcessor
	{
		public delegate Task<TextEmbedding> TextEmbeddingGenerator(string text);

		private readonly TextEmbeddingGenerator _embeddingGenerator;

		private readonly Configuration _options;


		public RAGProcessor(TextEmbeddingGenerator embeddingGenerator, IOptions<Configuration> options)
		{
			_embeddingGenerator = embeddingGenerator;
			_options = options.Value;
		}


		public Task<TextEmbedding> CreateEmbeddingsAsync(string text) => _embeddingGenerator(text);

		public async Task<RAGContext> CreateContext(string text)
		{
			var chuckSize = _options.ChuckSize;
			var textSpan = text.AsMemory();
			var chuckCount = text.Length / chuckSize + (text.Length % chuckSize == 0 ? 0 : 1);  
			var chunks = new TextChunk[chuckCount];
			
			for (int i = 0; i < chuckCount; i++)
			{
				var startIndex = i * chuckSize;
				var chunkText = text.AsMemory(startIndex, Math.Min(chuckSize + _options.ChuckOverlapSize, text.Length - startIndex)).ToString();
				var embedding = await CreateEmbeddingsAsync(chunkText);
				chunks[i] = new(chunkText, embedding);
			}

			return new(this, chunks);
		}


		public class Configuration
		{
			public int ChuckSize { get; init; } = 2000;

			public int ChuckOverlapSize { get; init; } = 200;
		}
	}
}