
using Newtonsoft.Json;
using System.Globalization;
using System.Runtime.Serialization;

namespace TelegramAIBot.Telemetry;

internal sealed class FileBasedTelemetryStorage : ITelemetryStorage
{
	private readonly Options _options;


	public FileBasedTelemetryStorage(IOptions<Options> options)
	{
		_options = options.Value;
	}


	public async Task CreateEntryAsync(string user, TelemetryEntry entry)
	{
		var directory = Path.Combine(_options.Path, user);
		if (Directory.Exists(directory) == false)
			Directory.CreateDirectory(directory);

		var now = DateTime.UtcNow;
		var fileName = now.ToString("dd_MM_yyyy") + ".telemetry";
		var fullFileName = Path.Combine(directory, fileName);

		using var writer = File.AppendText(fullFileName);

		await WriteEntryAsync(entry, now, writer);

		await writer.FlushAsync();
	}

	private static async Task WriteEntryAsync(TelemetryEntry entry, DateTime now, StreamWriter destination)
	{
		await destination.WriteAsync(now.ToString("hh:mm:ss MM/dd/yy: ", CultureInfo.InvariantCulture));
		var data = JsonConvert.SerializeObject(entry.Data, new JsonSerializerSettings() { Formatting = Formatting.None, Converters = [new Newtonsoft.Json.Converters.StringEnumConverter()] });
		await destination.WriteLineAsync(data);
	}


	public class Options
	{
		public required string Path { get; init; }
	}
}
