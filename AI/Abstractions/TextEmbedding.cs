namespace TelegramAIBot.AI.Abstractions
{
	internal readonly struct TextEmbedding
	{
		public TextEmbedding(ReadOnlyMemory<float> data)
		{
			Data = data;
		}

		
		public ReadOnlyMemory<float> Data { get; }


		public float this[int i] => Data.Span[i];


		public static float operator*(TextEmbedding left, TextEmbedding right)
		{
			if (left.Data.Length != right.Data.Length) 
				throw new ArgumentException("Sizes of embeddings are different");

			var len = right.Data.Length;
			var result = 0f;

			for (int i = 0; i < len; i++)
				result += left[i] * right[i];

			return result;
		} 
	}
}