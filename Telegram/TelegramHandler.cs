using System.Collections.Concurrent;
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
		private readonly ConcurrentDictionary<long, SemaphoreSlim> _sync = [];


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
			SemaphoreSlim? semaphore = null;

			try
			{
				switch (update.Type)
				{
					case UpdateType.Message:
						await waitSemaphoreAsync(update.Message!.Chat.Id, cancellationToken, out semaphore);
						if (cancellationToken.IsCancellationRequested) return;

						await _module.ProcessUserMessageAsync(update.Message!, cancellationToken);
						break;

					case UpdateType.CallbackQuery:
						await waitSemaphoreAsync(update.CallbackQuery!.From.Id, cancellationToken, out semaphore);
						if (cancellationToken.IsCancellationRequested) return;

						await _module.ProcessUserCallbackAsync(update.CallbackQuery!, cancellationToken);
						break;
				}
			}
			catch (Exception)
			{
				//TODO: add logging
			}
			finally
			{
				semaphore?.Release();
			}


			Task waitSemaphoreAsync(long chatId, CancellationToken cancellationToken, out SemaphoreSlim semaphore)
			{
				semaphore = _sync.GetOrAdd(chatId, (_) => new SemaphoreSlim(1));
				return semaphore.WaitAsync(cancellationToken);
			}
		}
	}
}
