using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot
{
	internal record class UserSettings(ChatCompletionOptions Options)
	{
		public const string StorageId = "userSettings";


		public UserSettings() : this(new ChatCompletionOptions())
		{

		}
	}
}
