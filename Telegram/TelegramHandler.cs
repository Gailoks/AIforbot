using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
namespace TelegramAIBot.Telegram 
{
    class TelegramHandler
    {
        TelegramBotClient _client;
        public TelegramHandler(TelegramBotClient client)
        {
            _client = client;
        }
        public void StartPooling()
        {
            _client.StartReceiving(HandleUpdates,HandleError);
        }
        private async Task HandleError(ITelegramBotClient botClient,Exception exception,CancellationToken cancellationToken)
        {
            return;
        }

		private async Task HandleUpdates(ITelegramBotClient botClient,Update update,CancellationToken cancellationToken)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                
                break;
                
                case UpdateType.CallbackQuery:
                
                break;

                default:
                break;
            }
        }
    }
}