using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramAIBot.AI.OpenAI;

namespace TelegramAIBot.Telegram
{
    internal class TelegramModule : TelegramModuleBase
    {   
        private TelegramBotClient _client;
        private Dictionary<long,TelegramAIBot.AI.OpenAI.Chat> _chats;
        private OpenAIClient _openAIClient;
        public TelegramModule(TelegramBotClient botClient, OpenAIClient client)
        {
            _client = botClient;
            _openAIClient = client;
        }
		public override Task ProcessUserPhoto(Message message, CancellationToken ct)
		{
			throw new NotImplementedException();
		}
		public override Task ProcessUserDefault(Message message, CancellationToken ct)
		{
			throw new NotImplementedException();
		}
		public override async Task ProcessUserNotCommand(Message message, CancellationToken ct)
		{
		    var text = message.Text;
            var id = message.Chat.Id;
            if(!_chats.ContainsKey(id)) await Start(id,ct);
            _chats[id].ModifyMessages(s => s.Add(new TelegramAIBot.AI.Abstractions.Message(TelegramAIBot.AI.Abstractions.MessageRole.User, new TelegramAIBot.AI.Abstractions.TextMessageContent(text))));
            var response = await _chats[id].CreateChatCompletionAsync();
            await _client.SendTextMessageAsync(id,response.Content.PresentAsString(),cancellationToken:ct);
		}

		public override async Task ProcessUserCallback(CallbackQuery callback, CancellationToken ct)
		{
			throw new NotImplementedException();
		}
		public override async Task ProcessUserDocument(Message message, CancellationToken ct)
		{
            await _client.SendTextMessageAsync(message.Chat.Id,"Not implemented yet",cancellationToken:ct);
		}
		
        public override async Task ProcessUserCommand(Message message,CancellationToken ct)
        {
            switch(message.Text.ToLower())
            {
                case "/start":
                await Start(message.Chat.Id, ct);
                break;

                case "/restart":
                await Restart(message.Chat.Id, ct);
                break;

                case "/settings":
                await Commands(message.Chat.Id, ct);
                break;

                default:
                await InvalidCommand(message.Chat.Id, ct);
                break;
            }
        }
        public async Task Start(long id, CancellationToken ct)
        {
            _chats[id] = new(_openAIClient);
            await _client.SendTextMessageAsync(id,"Bot started successfully",cancellationToken:ct);
        }
        public async Task Restart(long id, CancellationToken ct)
        {
            if(!_chats.ContainsKey(id))
            {
                _chats[id].Messages = [];
                await _client.SendTextMessageAsync(id,"Bot restarted successfully",cancellationToken:ct);
            } 
            else await Start(id,ct);
        }
        public async Task InvalidCommand(long id, CancellationToken ct)
        {
            await _client.SendTextMessageAsync(id,"nvalid command",cancellationToken:ct);
        }
         
        public async Task  Commands(long id,CancellationToken ct)
        {
            await _client.SendTextMessageAsync(id,"/start\n /restart\n /settings\n",cancellationToken:ct);
        }
    }
}