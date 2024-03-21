using System.Collections.Concurrent;
using Telegram.Bot;
using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.Telegram;

using TGMessage = Telegram.Bot.Types.Message;
using AIMessage = TelegramAIBot.AI.Abstractions.Message;

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
		}


		public override async Task HandleTextMessageAsync(TGMessage message, string text, CancellationToken ct)
		{
			var chat = GetChat(message.Chat.Id);

			chat.ModifyMessages(s => s.Add(new AIMessage(MessageRole.User, new TextMessageContent(text))));

			var response = await chat.CreateChatCompletionAsync();
			chat.ModifyMessages(s => s.Add(response));

			await Client.NativeClient.SendTextMessageAsync(message.Chat, response.Content.PresentAsString(), cancellationToken: ct);
		}

		public async Task Start(TGMessage message, CancellationToken ct)
		{
			_chats.TryRemove(message.Chat.Id, out _);
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Bot started successfully", cancellationToken: ct);
		}

		public async Task Restart(TGMessage message, CancellationToken ct)
		{
			_chats.TryRemove(message.Chat.Id, out _);
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Bot restarted successfully", cancellationToken: ct);
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
	}
}