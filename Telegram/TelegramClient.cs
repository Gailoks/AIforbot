using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramAIBot.Telegram;

class TelegramClient(IOptions<TelegramClient.Options> options, ITelegramEventHandler handler, ILogger<TelegramClient>? logger = null)
{
    public const char CommandPrefix = '/';

    private readonly ITelegramEventHandler _handler = handler;
    private readonly ILogger? _logger = logger;
    public TelegramBotClient Client { get; } = new(options.Value.Token);

    public void Start()
    {
        Client.StartReceiving(HandleUpdateAsync, HandleExceptionAsync, new ReceiverOptions()
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        });

        _logger?.LogInformation("Bot started receiving");
        Thread.Sleep(-1);   
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        _logger?.LogTrace("An update received: {Update}", JsonConvert.SerializeObject(update));
        switch (update.Type)
        {
            case UpdateType.Message:
                Message message = update.Message!;
                if (message.Type == MessageType.Text && message.Text !.StartsWith(CommandPrefix))
                    await _handler.HandleCommandAsync(message.Text[1..], message);
                else await _handler.HandleMessageAsync(message);
                break;
            case UpdateType.CallbackQuery:

                CallbackQuery callbackQuery = update.CallbackQuery!;
                await _handler.HandleButtonAsync(callbackQuery.Message!, callbackQuery.Data!);
                break;
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