using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
namespace TelegramAIBot
{
	class Bot(string token)
	{
        readonly private TelegramBotClient _client = new TelegramBotClient(token);
        private ConcurrentDictionary<long,OpenAI.Chat> _chats;

		public void StartReceiving()
        {
            _client.StartReceiving(Update, Error);
        }
        async Task Update(ITelegramBotClient botClient,Update update,CancellationToken ct)
        {
            if(update.CallbackQuery != null)
            {
                await CallbackQueryHandler(update.CallbackQuery, ct);
            }
            else if(update.Message != null)
            {
                await MessageHandler( update.Message, ct);
            }
            return;
        }
        async Task CallbackQueryHandler(CallbackQuery callbackQuery, CancellationToken ct)
        {
            return;
        }
        async Task Start(long id, CancellationToken ct)
        {
            _chats[id] = new OpenAI.Chat();
            await _client.SendTextMessageAsync(id, "Bot started succesful", cancellationToken:ct);
            return;
        }
        async Task Restart(long id, CancellationToken ct)
        {
            if(_chats.ContainsKey(id))
            {
                _chats[id].Clear();
                await _client.SendTextMessageAsync(id, "Bot restarted succesful", cancellationToken:ct);
            }
            else await Start(id, ct);
            return;
        }
        async Task Info(long id, CancellationToken ct)
        {
            await _client.SendTextMessageAsync(id, "Some info", cancellationToken:ct);
            return;
        }
        async Task Settings(long id, CancellationToken ct)
        {
            await _client.SendTextMessageAsync(id, "Some settings", cancellationToken:ct);
        }
        async Task CommandHandler(Message message, CancellationToken ct)
        {
            long id = message.Chat.Id;
            switch(message.Text.ToLower())
            {
                case "/start":
                    await Start(id,ct);
                    break;
                case "/info":
                    await Info(id,ct);
                    break;
                case "/restart":
                    await Restart(id,ct);
                    break;
                case "/settings":
                    await Settings(id,ct);
                    break;
                default:
					await _client.SendTextMessageAsync(message.Chat.Id, "Wrong command", cancellationToken: ct);
                break;
            }
            return;
        }
        async Task MessageHandler(Message message,CancellationToken ct)
        {
            if(message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            {
                /* Commands */
                if(message.Text.StartsWith('/'))
                {
                    await CommandHandler(message,ct);
                }
                /* Usual prompt*/
                else
                {

                }
            }
            else if(message.Type == Telegram.Bot.Types.Enums.MessageType.Photo)
            {
                /* clip model*/

            }
            else if(message.Type == Telegram.Bot.Types.Enums.MessageType.Document)
            {
                /* RAG */

            }
            else
            {
                await _client.SendTextMessageAsync(message.Chat.Id,"I can't answer to that!",cancellationToken:ct);
            }
            return;
        }
        private Task Error(ITelegramBotClient tbc, Exception exception,CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}