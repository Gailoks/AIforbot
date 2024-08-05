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
using Telegram.Bot.Types.ReplyMarkups;
using System.Globalization;
using Telegram.Bot.Types.Enums;


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
        await Client.SendTextMessageAsync(message.Chat, _localizer.Get((await _userRepository.GetAsync(message.From!.Id)).CultureInfo, "HelpContent"));
        yield break;
    }

    [TelegramSequence(typeof(CommandCondition), "system")]
    public async IAsyncEnumerator<WaitCondition> ProcessCommandSystem(TGMessage message, SequenceTrigger trigger)
    {
        long userId = message.From!.Id;
        if (_sessions.TryGetValue(userId, out var session) == false)
            yield break;
        var systemWaitCondition = new TextMessageCondition();

        await Client.SendTextMessageAsync(userId, _localizer.Get((await _userRepository.GetAsync(userId)).CultureInfo, "WaitingNewSystemPrompt"));

        yield return systemWaitCondition;

        session.SystemPrompt = systemWaitCondition.CapturedMessage.Text;

        await Client.SendTextMessageAsync(userId, _localizer.Get((await _userRepository.GetAsync(userId)).CultureInfo, "SystemPromptSet"));
    }

    [TelegramSequence(typeof(CommandCondition), "language")]
    public async IAsyncEnumerator<WaitCondition> ProcessCommandLanguageAsync(TGMessage message, SequenceTrigger trigger)
    {
        InlineKeyboardMarkup keyboard = new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("en"),
                InlineKeyboardButton.WithCallbackData("ru")
            }

        };
        long userId = message.From!.Id;
        var messageId = await Client.SendTextMessageAsync(userId, _localizer.Get((await _userRepository.GetAsync(userId)).CultureInfo, "LanguageSettings"), replyMarkup: keyboard);
        while (true)
        {
            var languageWaitCondition = new ButtonCondition(messageId.MessageId);
            yield return languageWaitCondition;
            if ((await _userRepository.GetAsync(userId)).CultureInfo != new CultureInfo(languageWaitCondition.ButtonPressed))
            {
                await _userRepository.SetAsync(new(userId) { CultureInfo = new(languageWaitCondition.ButtonPressed) });
                await Client.EditMessageTextAsync(userId, messageId.MessageId, _localizer.Get((await _userRepository.GetAsync(userId)).CultureInfo, "LanguageSettings"), replyMarkup:keyboard);
            }
        }
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
        await Client.SendTextMessageAsync(message.Chat, _localizer.Get(userProfile.CultureInfo, "SessionBegin"));
        yield break;
    }


    [TelegramSequence(typeof(TextMessageCondition))]
    public async IAsyncEnumerator<WaitCondition> ProcessTextMessage(TGMessage message, SequenceTrigger trigger)
    {
        long userId = message.From!.Id;
        if (_sessions.TryGetValue(userId, out var session) == false)
            yield break;

		var question = message.Text!;
		var task = session.AskAsync(question);

		while (true)
		{
			await Client.SendChatActionAsync(userId, ChatAction.Typing);
			await Task.WhenAny(task, Task.Delay(4000));
			if (task.IsCompleted)
				break;
		}

		await Client.SendTextMessageAsync(userId, task.Result); // TODO: for other types
    }
}