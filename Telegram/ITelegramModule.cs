using Telegram.Bot.Types;

namespace TelegramAIBot.Telegram
{
	internal interface ITelegramModule
	{
		public Task ProcessUserMessageAsync(Message message, CancellationToken ct);

		public Task ProcessUserCallbackAsync(CallbackQuery callback, CancellationToken ct);

		public void BindClient(TelegramClient client);
	}   
}