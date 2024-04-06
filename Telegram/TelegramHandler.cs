using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramAIBot.Telegram
{
	class TelegramHandler
	{
		public static readonly EventId PoolingIsStartedLOG = new EventId(11, nameof(PoolingIsStartedLOG)).Form();
		public static readonly EventId TelegramUpdateReceivedLOG = new EventId(12, nameof(TelegramUpdateReceivedLOG)).Form();
		public static readonly EventId TelegramErrorLOG = new EventId(13, nameof(TelegramErrorLOG)).Form();
		public static readonly EventId ProcessingErrorLOG = new EventId(13, nameof(TelegramErrorLOG)).Form();


		private readonly TelegramBotClient _client;
		private readonly ITelegramModule _module;
		private readonly ConcurrentDictionary<long, SemaphoreSlim> _sync = [];
		private readonly ILogger? _logger;


		public TelegramHandler(TelegramBotClient client, ITelegramModule module, ILogger? logger)
		{
			_client = client;
			_module = module;
			_logger = logger;
		}


		public void StartPooling()
		{
			ReceiverOptions receiverOptions = new()
			{
				AllowedUpdates = []
			};

			_client.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions);

			_logger?.Log(LogLevel.Information, PoolingIsStartedLOG, "TelegramHandler is now receiving events from telegram");
		}

		private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
		{
			_logger?.Log(LogLevel.Error, TelegramErrorLOG, exception, "While telegram client an error was occupied");
			return Task.CompletedTask;
		}

		private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			HandleUpdateInternalAsync(update, cancellationToken);
			_logger?.Log(LogLevel.Debug, TelegramUpdateReceivedLOG, "Telegram update of type {Type} has been received and is processing now", update.Type);
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
			catch (Exception ex)
			{
				_logger?.Log(LogLevel.Error, ProcessingErrorLOG, ex, "Error during processing telegram update");
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
