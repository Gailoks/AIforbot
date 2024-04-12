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

		public TextEmbedding normalize()
		{
			var length = MathF.Sqrt(this * this);
			return this / length;
		}
		public static float cos_distance(TextEmbedding left, TextEmbedding right)
		{
			if (left.Data.Length != right.Data.Length)
				throw new ArgumentException("Sizes of embeddings are different");
			left = left.normalize();
			right = right.normalize();
			var result = 1f - left * right;
			return result;
		}
		public static TextEmbedding operator/(TextEmbedding left, float right) => left * (1/right);
		public static TextEmbedding operator*(TextEmbedding left, float right)
		{
			float[] result = new float[left.Data.Length];

			for(int i = 0; i < left.Data.Length; i++)
			{
				result[i] = left[i] * right;
			}
			return new TextEmbedding(result);
		}
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