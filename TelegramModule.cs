using System.Collections.Concurrent;
using Telegram.Bot;
using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.Telegram;
using Telegram.Bot.Types.Enums;
using TGMessage = Telegram.Bot.Types.Message;
using AIMessage = TelegramAIBot.AI.Abstractions.Message;
using File = Telegram.Bot.Types.File;
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

			chat.ModifyMessages(s => s.Add(new AIMessage(MessageRole.User, [new TextMessageContent(text)])));

			await CreateCompletionAndRespondAsync(message.Chat, chat, ct);
		}

		public override async Task HandlePhotoAsync(TGMessage message, PhotoSize[] photos, CancellationToken ct)
		{
			var photo = photos.OrderByDescending(s => s.Width * s.Height).First();

			File fileInfo = await Client.NativeClient.GetFileAsync(photo.FileId, ct);
			var url = $"https://api.telegram.org/file/bot{Client.ActiveConfiguration.Token}/{fileInfo.FilePath}";
			IChat chat = GetChat(message.Chat.Id);

			var caption = message.Caption;

			chat.ModifyMessages(s =>
			{
				if (caption is not null)
					return s.Add(new AIMessage(MessageRole.User, [new ImageMessageContent(url), new TextMessageContent(caption)]));
				else return s.Add(new AIMessage(MessageRole.User, new ImageMessageContent(url)));
			});

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
			var response = await chat.CreateChatCompletionAsync();
			chat.ModifyMessages(s => s.Add(response));

			await Client.NativeClient.SendTextMessageAsync(tgChat, response.Contents[0].PresentAsString(), cancellationToken: ct, parseMode: ParseMode.Markdown);
		}
	}
}