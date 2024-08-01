using TelegramAIBot.Telegram.Sequences;
using TelegramAIBot.Telegram;
using TelegramAIBot.Telegram.Sequences.Conditions;
using Telegram.Bot.Types;
using Telegram.Bot;


namespace TelegramAIBot;

internal class TelegramModule : ITelegramSequenceModule
{
        private TelegramClient? _client;


	private TelegramClient Client => _client ?? throw new InvalidOperationException("Bind before use");


	public void BindClient(TelegramClient client) => _client = client;


	[TelegramSequence(typeof(CommandCondition), "start")]
    public async IAsyncEnumerator<WaitCondition> ProcessCommandStartAsync(Message message, SequenceTrigger trigger)
    {
        await Client.Client.SendTextMessageAsync(message.Chat,"Hello");
        TextMessageCondition dataWaitCondition = new TextMessageCondition();
        yield return dataWaitCondition;
        await Client.Client.SendTextMessageAsync(message.Chat,dataWaitCondition.CapturedMessage.Text!);
        yield break;
    }    
}