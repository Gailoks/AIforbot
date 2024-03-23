using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramAIBot.Telegram.Keyboards
{
	class SettingsKeyboard
	{
		public InlineKeyboardMarkup KeyboardMarkup{get;} = new(new[]
		{
			new[]
			{
				InlineKeyboardButton.WithCallbackData("Change system prompt","change system prompt")
			},
			new[]
			{
				InlineKeyboardButton.WithCallbackData("Change temp","change temp")
			},
			new[]
			{
				InlineKeyboardButton.WithCallbackData("Change top p","change top p")
			},
			new[]
			{
				InlineKeyboardButton.WithCallbackData("Change frequency penalty","change frequency penalty")
			}
		});
	}
}