using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramAIBot.Telegram
{
    internal abstract class TelegramModuleBase : ITelegramModule
    {
        public async Task ProcessUserMessage(Message message, CancellationToken ct)
        {
            switch(message.Type)
            {
                case MessageType.Text:
                if(message.Text.StartsWith('/')) await ProcessUserCommand(message, ct);
                else await ProcessUserNotCommand(message, ct);
                break;
                case MessageType.Document:
                await ProcessUserDocument(message, ct);
                break;
                case MessageType.Photo:
                await ProcessUserPhoto(message, ct);
                break;
                default:
                await ProcessUserDefault(message, ct);
                break;
            }
        }
        public abstract Task ProcessUserCommand(Message message,CancellationToken ct);
        public abstract Task ProcessUserNotCommand(Message message,CancellationToken ct);
        public abstract Task ProcessUserDocument(Message message,CancellationToken ct);
        public abstract Task ProcessUserPhoto(Message message,CancellationToken ct);
        public abstract Task ProcessUserDefault(Message message,CancellationToken ct);
        public abstract Task ProcessUserCallback(CallbackQuery callback,CancellationToken ct);
    }
}