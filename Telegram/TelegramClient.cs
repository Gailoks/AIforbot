using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramAIBot.Telegram;

class TelegramClient(IOptions<TelegramClient.Options> options, ITelegramEventHandler handler, ILogger<TelegramClient>? logger = null) : TelegramBotClient(options.Value.Token)
{
    public const char CommandPrefix = '/';

    private readonly ConcurrentDictionary<long, SemaphoreSlim> _sync = [];
    private readonly ITelegramEventHandler _handler = handler;
    private readonly ILogger? _logger = logger;
    public void Start()
    {
        this.StartReceiving(HandleUpdateAsync, HandleExceptionAsync, new ReceiverOptions()
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        });

        _logger?.LogInformation("Bot started receiving");
    }

    private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        HandleInternalUpdateAsync(update);
        return Task.CompletedTask;
    }

    private async void HandleInternalUpdateAsync(Update update)
    {
        SemaphoreSlim? semaphore = null;
        _logger?.LogTrace("An update received: {Update}", JsonConvert.SerializeObject(update));
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    Message message = update.Message!;
                    await waitSemaphoreAsync(message.Chat.Id, out semaphore);
                    if (message.Type == MessageType.Text && message.Text!.StartsWith(CommandPrefix))
                        await _handler.HandleCommandAsync(message.Text[1..], message);
                    else await _handler.HandleMessageAsync(message);
                    break;
                case UpdateType.CallbackQuery:

                    CallbackQuery callbackQuery = update.CallbackQuery!;
                    await waitSemaphoreAsync(callbackQuery.From.Id, out semaphore);
                    await _handler.HandleButtonAsync(callbackQuery.Message!, callbackQuery.Data!);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception during processing update");
        }
        finally
        {
            semaphore?.Release();
        }
        Task waitSemaphoreAsync(long chatId, out SemaphoreSlim semaphore)
        {
            semaphore = _sync.GetOrAdd(chatId, (_) => new SemaphoreSlim(1));
            return semaphore.WaitAsync();
        }
    }

    private Task HandleExceptionAsync(ITelegramBotClient client, Exception exception, CancellationToken ct)
    {
        logger?.LogError(exception, "Exception in telegram client");
        return Task.CompletedTask;
    }

    public class Options
    {
        public required string Token { get; init; }

    }
}