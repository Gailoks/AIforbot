using TelegramAIBot.AI.Abstractions;

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
		public override string ToString()
		{
			var text = _chunks[0].Text;
			return string.Concat(text.AsSpan(0,Math.Min(text.Length,45)), "...");
		}


		
		public async Task<TextChunk> AssociateChunkAsync(string prompt)
		{
			var embedding = await _processor.CreateEmbeddingsAsync(prompt);
			var resultChunk = _chunks[0];
			var minCoherence = TextEmbedding.cos_distance(_chunks[0].Embedding , embedding);
			foreach(var chunk in _chunks.Skip(1))
			{
				//var coherence = chunk.Embedding * embedding; //Changing func 
				var coherence = TextEmbedding.cos_distance(chunk.Embedding, embedding); 
				if (coherence < minCoherence)
				{
					minCoherence = coherence;
					resultChunk = chunk;
				}
			}

			return resultChunk;
		}
	}
}