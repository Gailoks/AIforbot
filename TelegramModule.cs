using System.Collections.Concurrent;
using Telegram.Bot;
using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.Telegram;
using Telegram.Bot.Types.Enums;
using TGMessage = Telegram.Bot.Types.Message;
using AIMessage = TelegramAIBot.AI.Abstractions.Message;
using Telegram.Bot.Types;

namespace TelegramAIBot
{
	internal class TelegramModule : TelegramModuleBase
	{
		private readonly ConcurrentDictionary<long, IChat> _chats = [];
		private readonly IAIClient _aiClient;


		public TelegramModule(IAIClient aiClient)
		{
			_aiClient = aiClient;

			RegisterCommand("start", Start);
			RegisterCommand("restart", Restart);
			RegisterCommand("settings",Settings);
		}


		public override async Task HandleTextMessageAsync(TGMessage message, string text, CancellationToken ct)
		{
			var chat = GetChat(message.Chat.Id);

			chat.ModifyMessages(s => s.Add(new AIMessage(MessageRole.User, new TextMessageContent(text))));

			await CreateCompletionAndRespondAsync(message.Chat, chat, ct);
		}

		public async Task Start(TGMessage message, CancellationToken ct)
		{
			_chats.TryRemove(message.Chat.Id, out _);
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Bot started successfully", cancellationToken: ct);
		}

		public async Task Restart(TGMessage message, CancellationToken ct)
		{
			GetChat(message.Chat.Id).ModifyMessages(s => s.Clear());
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Bot restarted successfully", cancellationToken: ct);
		}

		public async Task Settings(TGMessage message, CancellationToken ct)
		{
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Here is some settings", replyMarkup: new Telegram.Keyboards.SettingsKeyboard().KeyboardMarkup, cancellationToken: ct);
		}

		private IChat GetChat(long userId)
		{
			return _chats.GetOrAdd(userId, (userID) =>
			{
				var chat = _aiClient.CreateChat();

				chat.Options = new(
					ModelName: "gpt-3.5-turbo",
					SystemPrompt: "You are useful assistance"
				);

				return chat;
			});
		}

		private async Task CreateCompletionAndRespondAsync(ChatId tgChat, IChat chat, CancellationToken ct)
		{
			var preMessage = await Client.NativeClient.SendTextMessageAsync(tgChat, "Generating, please wait", cancellationToken: ct);

			var task = chat.CreateChatCompletionAsync();

			await Task.WhenAll(Task.Delay(2000, ct), task);

			var response = task.Result;
			chat.ModifyMessages(s => s.Add(response));

			await Client.NativeClient.EditMessageTextAsync(tgChat, preMessage.MessageId, response.Content.PresentAsString(), cancellationToken: ct, parseMode: ParseMode.Markdown);
		}
	}
}