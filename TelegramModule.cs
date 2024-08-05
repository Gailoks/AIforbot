using TelegramAIBot.Telegram.Sequences;
using TelegramAIBot.Telegram;
using TelegramAIBot.Telegram.Sequences.Conditions;
using Telegram.Bot;
using TelegramAIBot.AI.Abstractions;
using TGMessage = Telegram.Bot.Types.Message;
using TelegramAIBot.Telemetry;
using Microsoft.Extensions.Localization;
using TelegramAIBot.User;
using System.Collections.Concurrent;


namespace TelegramAIBot;

internal class TelegramModule(IAIClient aiClient, ITelemetryStorage telemetry, IStringLocalizer<TelegramModule> localizer, IUserRepository userRepository) : ITelegramSequenceModule
{
    private readonly IAIClient _aiClient = aiClient;
    private readonly ITelemetryStorage _telemetry = telemetry;
    private readonly IStringLocalizer<TelegramModule> _localizer = localizer;

    private readonly ConcurrentDictionary<long, Session> _sessions = [];

    private IUserRepository _userRepository = userRepository;
    private TelegramClient? _client;


    private TelegramClient Client => _client ?? throw new InvalidOperationException("Bind before use");


    public void BindClient(TelegramClient client) => _client = client;


    [TelegramSequence(typeof(CommandCondition), "help")]
    public async IAsyncEnumerator<WaitCondition> ProcessCommandHelpAsync(TGMessage message, SequenceTrigger trigger)
    {
        await Client.SendTextMessageAsync(message.Chat, _localizer.Get(message.GetUserLocale(), "HelpContent"));
        yield break;
    }

    [TelegramSequence(typeof(CommandCondition), "start")]
    public async IAsyncEnumerator<WaitCondition> ProcessCommandStartAsync(TGMessage message, SequenceTrigger trigger)
    {
        long userId = message.From!.Id;
        if (!await _userRepository.ContainsAsync(userId))
        {
            await _userRepository.SetAsync(new(userId) { CultureInfo = message.GetUserLocale() });
        }
        var userProfile = await _userRepository.GetAsync(userId);
        if (_sessions.TryRemove(userId, out var old))
        {
            await old.WriteTelemetryAsync(_telemetry, userId);
        }
        _sessions.TryAdd(userId, new(_aiClient.CreateChat()));
        await Client.SendTextMessageAsync(message.Chat, _localizer.Get(message.GetUserLocale(), "SessionBegin"));
        yield break;
    }


    [TelegramSequence(typeof(TextMessageCondition))]
    public async IAsyncEnumerator<WaitCondition> ProcessTextMessage(TGMessage message, SequenceTrigger trigger)
    {
        long userId = message.From!.Id;
        if (_sessions.TryGetValue(userId, out var session) == false)
            yield break;
        await Client.SendTextMessageAsync(userId, await session.AskAsync(message.Text!)); // TODO: for other types
    }


}