using TelegramAIBot.Telegram.Sequences;
using TelegramAIBot.Telegram;
using TelegramAIBot.Telegram.Sequences.Conditions;
using Telegram.Bot.Types;
using Telegram.Bot;
using TelegramAIBot.AI.Abstractions;
using TGMessage = Telegram.Bot.Types.Message;
using Message = TelegramAIBot.AI.Abstractions.Message;
using Telegram.Bot.Types.Enums;


namespace TelegramAIBot;

internal class TelegramModule(IAIClient aiClient) : ITelegramSequenceModule
{
    private readonly IAIClient _aiClient = aiClient;
    private TelegramClient? _client;


    private TelegramClient Client => _client ?? throw new InvalidOperationException("Bind before use");


    public void BindClient(TelegramClient client) => _client = client;


    [TelegramSequence(typeof(CommandCondition), "start")]
    public async IAsyncEnumerator<WaitCondition> ProcessCommandStartAsync(TGMessage message, SequenceTrigger trigger)
    {
        await Client.SendTextMessageAsync(message.Chat, "New session started, .exit to finish");
        var aiChat = _aiClient.CreateChat();


        while (true)
        {
            var waitCondition = new TextMessageCondition();
            yield return waitCondition;
            string text = waitCondition.CapturedMessage.Text!;

            aiChat.Messages.Add(new Message(MessageRole.User, text));

            var newMessageTask = aiChat.CreateChatCompletionAsync();

            while (true)
            {
                await Client.SendChatActionAsync(waitCondition.CapturedMessage.Chat, ChatAction.Typing);
                await Task.WhenAny(newMessageTask, Task.Delay(4000));
                if (newMessageTask.IsCompleted)
                    break;
            }

            await Client.SendTextMessageAsync(message.Chat, newMessageTask.Result.Content);
        }
    }
}