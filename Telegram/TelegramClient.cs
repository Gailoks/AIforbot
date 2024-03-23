using Telegram.Bot;

namespace TelegramAIBot.Telegram
{
	internal sealed class TelegramClient
	{
		public const string CommandPrefix = "/";


		private readonly Configuration _configuration;
		private readonly ITelegramModule _module;
		private readonly TelegramHandler _handler;
		private readonly TelegramBotClient _nativeClient;


		public TelegramClient(Configuration configuration, ITelegramModule module)
		{
			_configuration = configuration;
			_module = module;
			_nativeClient = new TelegramBotClient(configuration.Token);
			_handler = new TelegramHandler(_nativeClient, module);
		}


		public TelegramBotClient NativeClient => _nativeClient;

		public Configuration ActiveConfiguration => _configuration;


		public void Start()
		{
			_module.BindClient(this);
			_handler.StartPooling();
		}


		public class Configuration
		{
			public required string Token { get; init; }
		}
	}
}
