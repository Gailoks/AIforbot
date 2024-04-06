using System.Text;

namespace TelegramAIBot
{
	internal sealed class TextExtractor
	{
		private static readonly Dictionary<string, TextExtractorUnit> _units = new()
		{
			["application/pdf"] = ProcessPDF,
			["text/plain"] = ProcessPlainText
		};


		public TextExtractor()
		{

		}


		public string Extract(ReadOnlySpan<byte> data, string mimeType)
		{
			if (_units.TryGetValue(mimeType, out var unit))
				return unit(data);
			else throw new NotSupportedException($"Type {mimeType} is not supported to be converted to plain text");
		}


		private static string ProcessPDF(ReadOnlySpan<byte> data)
		{
			throw new NotImplementedException();
		}

		private static string ProcessPlainText(ReadOnlySpan<byte> data)
		{
			return Encoding.UTF8.GetString(data);
		}


		private delegate string TextExtractorUnit(ReadOnlySpan<byte> data);
	}
}
