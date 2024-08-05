using TelegramAIBot.Telegram.Sequences;
using TelegramAIBot.Telegram;
using TelegramAIBot.Telegram.Sequences.Conditions;
using Telegram.Bot;
using TelegramAIBot.AI.Abstractions;
using TGMessage = Telegram.Bot.Types.Message;
using Message = TelegramAIBot.AI.Abstractions.Message;
using Telegram.Bot.Types.Enums;
using TelegramAIBot.Telemetry;
using Microsoft.Extensions.Localization;


namespace TelegramAIBot;

internal class TelegramModule(IAIClient aiClient, ITelemetryStorage telemetry, IStringLocalizer<TelegramModule> localizer) : ITelegramSequenceModule
{
    private readonly IAIClient _aiClient = aiClient;
	private readonly ITelemetryStorage _telemetry = telemetry;
	private readonly IStringLocalizer<TelegramModule> _localizer = localizer;
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
		IAIChat? aiChat = null;

		try
		{
			await Client.SendTextMessageAsync(message.Chat, _localizer.Get(message.GetUserLocale(), "SessionBegin"));
			aiChat = _aiClient.CreateChat();


			while (true)
			{
				var waitCondition = new TextMessageCondition();
				yield return waitCondition;
				string text = waitCondition.CapturedMessage.Text!;

				if (text.StartsWith('!'))
				{
					aiChat.ModifyOptions(s => s with { SystemPrompt = text[1..] });
					await Client.SendTextMessageAsync(message.Chat, _localizer.Get(message.GetUserLocale(), "SystemPromptSet"));
					continue;
				}

				aiChat.Messages.Add(new Message(MessageRole.User, text));

				var newMessageTask = aiChat.CreateChatCompletionAsync();

				while (true)
				{
					await Client.SendChatActionAsync(waitCondition.CapturedMessage.Chat, ChatAction.Typing);
					await Task.WhenAny(newMessageTask, Task.Delay(4000));
					if (newMessageTask.IsCompleted)
						break;
				}

				try
				{
					await Client.SendTextMessageAsync(message.Chat, newMessageTask.Result.Content, parseMode: ParseMode.Markdown);
				}
				catch
				{
					await Client.SendTextMessageAsync(message.Chat, newMessageTask.Result.Content);
				}
			}
		}
		finally
		{
			if (aiChat is not null)
			{
				var entry = new Dictionary<string, object?>()
				{
					["messages"] = aiChat.Messages.Select(s => new { role = s.Role, content = s.Content }).ToArray(),
					["system"] = aiChat.Options.SystemPrompt
				};
				await _telemetry.CreateEntryAsync(message.Chat.Id.ToString(), new TelemetryEntry(entry));
			}
		}
    }
}