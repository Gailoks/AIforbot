using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
namespace TelegramAIBot.Telegram 
{
	class TelegramHandler
	{
		private readonly TelegramBotClient _client;
		private readonly ITelegramModule _module;


		public TelegramHandler(TelegramBotClient client, ITelegramModule module)
		{
			_client = client;
			_module = module;
		}


		public void StartPooling()
		{
			ReceiverOptions receiverOptions = new()
			{
				AllowedUpdates = []
			};

			_client.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions);
		}

		private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
		{
			//TODO: add logging
			return Task.CompletedTask;
		}

		private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			HandleUpdateInternalAsync(update, cancellationToken);
			return Task.CompletedTask;
		}

		private async void HandleUpdateInternalAsync(Update update, CancellationToken cancellationToken)
		{
			try
			{
				switch (update.Type)
				{
					case UpdateType.Message:
						await _module.ProcessUserMessageAsync(update.Message!, cancellationToken);
						break;

					case UpdateType.CallbackQuery:
						await _module.ProcessUserCallbackAsync(update.CallbackQuery!, cancellationToken);
						break;
				}
			}
			catch (Exception)
			{
				//TODO: add logging
			}
		}
	}
}