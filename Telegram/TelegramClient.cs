using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace TelegramAIBot.Telegram
{
	internal sealed class TelegramClient
	{
		public static readonly EventId ClientStartedLOG = new EventId(11, nameof(ClientStartedLOG)).Form();


		public const string CommandPrefix = "/";


		private readonly Configuration _configuration;
		private readonly ITelegramModule _module;
		private readonly ILogger<TelegramClient>? _logger;
		private readonly TelegramHandler _handler;
		private readonly TelegramBotClient _nativeClient;


		public TelegramClient(IOptions<Configuration> configuration, ITelegramModule module, ILogger<TelegramClient>? logger = null)
		{
			_configuration = configuration.Value;
			_module = module;
			_logger = logger;
			_nativeClient = new TelegramBotClient(configuration.Value.Token);
			_handler = new TelegramHandler(_nativeClient, module, logger);
		}


		public TelegramBotClient NativeClient => _nativeClient;

		public Configuration ActiveConfiguration => _configuration;


		public void Start()
		{
			_module.BindClient(this);
			_handler.StartPooling();

			_logger?.Log(LogLevel.Information, ClientStartedLOG, "Telegram client is started successfully");
		}


		public class Configuration
		{
			public required string Token { get; init; }
		}
	}
}
