namespace TelegramAIBot.RAG
{
	internal sealed class RAGContext
	{
		private readonly RAGProcessor _processor;
		private readonly IReadOnlyList<TextChunk> _chunks;

		public RAGContext(RAGProcessor processor, IReadOnlyList<TextChunk> chunks)
		{
			_processor = processor;
			_chunks = chunks;
		}

		
		public async Task<TextChunk> AssociateChunkAsync(string prompt)
		{
			var embedding = await _processor.CreateEmbeddingsAsync(prompt);
			var resultChunk = _chunks[0];
			var maxCoherence = _chunks[0].Embedding * embedding;
			foreach(var chunk in _chunks.Skip(1))
			{
				var coherence = chunk.Embedding * embedding;
				if (coherence > maxCoherence)
				{
					maxCoherence = coherence;
					resultChunk = chunk;
				}
			}

			return resultChunk;
		}
	}
}