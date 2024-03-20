using Telegram.Bot.Types;

namespace TelegramAIBot.Telegram
{
    internal interface ITelegramModule
    {
       public Task ProcessUserMessage(Message message, CancellationToken ct);
       public Task ProcessUserCallback(CallbackQuery callback, CancellationToken ct);
    }   
}