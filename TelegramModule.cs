using System.Collections.Concurrent;
using Telegram.Bot;
using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.Telegram;
using Telegram.Bot.Types.Enums;
using TGMessage = Telegram.Bot.Types.Message;
using AIMessage = TelegramAIBot.AI.Abstractions.Message;
using Telegram.Bot.Types;
using TelegramAIBot.UserData;
using Telegram.Bot.Types.ReplyMarkups;
using FluentValidation;
using System.Globalization;

namespace TelegramAIBot
{
	internal class TelegramModule : TelegramModuleBase
	{
		private readonly ConcurrentDictionary<long, UserState> _users = [];
		private readonly IAIClient _aiClient;
		private readonly IUserDataRepository _userDataRepository;
		private readonly IValidator<ChatCompletionOptions> _chatCompletionOptionsValidator = new ChatCompletionOptionsValidator();


		private readonly InlineKeyboardMarkup _settingsKeyboardMarkup = new(
		[
			[InlineKeyboardButton.WithCallbackData("Change system prompt", "Ssystem_prompt")],
			[InlineKeyboardButton.WithCallbackData("Change temp", "Stemperature")],
			[InlineKeyboardButton.WithCallbackData("Change top p", "Stop_p")],
			[InlineKeyboardButton.WithCallbackData("Change frequency penalty", "Sfrequency_penalty")]
		]);


		public TelegramModule(IAIClient aiClient, IUserDataRepository userDataRepository)
		{
			_aiClient = aiClient;
			_userDataRepository = userDataRepository;

			RegisterCommand("start", Start);
			RegisterCommand("restart", Restart);
			RegisterCommand("settings",Settings);
		}


		protected override async Task HandleTextMessageAsync(TGMessage message, string text, CancellationToken ct)
		{
			var state = GetState(message.Chat.Id);

			if (state.ActiveParameterToChange is not null)
			{
				using var holder = _userDataRepository.Get<UserSettings>(UserSettings.StorageId);
				try
				{
					var so = holder.Object;
					var culture = new CultureInfo("en");

					switch (state.ActiveParameterToChange)
					{
						case "system_prompt":
							holder.Object = so with { Options = so.Options with { SystemPrompt = text } };
							break;

						case "temperature":
							holder.Object = so with { Options = so.Options with { Temperature = double.Parse(text, culture) } };
							break;

						case "top_p":
							holder.Object = so with { Options = so.Options with { TopP = double.Parse(text, culture) } };
							break;

						case "frequency_penalty":
							holder.Object = so with { Options = so.Options with { FrequencyPenalty = double.Parse(text, culture) } };
							break;

						default:
							break;
					}

					_chatCompletionOptionsValidator.ValidateAndThrow(holder.Object.Options);

					await Client.NativeClient.SendTextMessageAsync(message.Chat, "Change ok", cancellationToken: ct);
				}
				catch (Exception)
				{
					await Client.NativeClient.SendTextMessageAsync(message.Chat, "Invalid parameter value", cancellationToken: ct);
				}

				state.ActiveParameterToChange = null;
				return;
			}

			await state.OperationSync.WaitAsync(ct);
			if (ct.IsCancellationRequested) return;

			try
			{
				var chat = state.Chat;

				chat.ModifyMessages(s => s.Add(new AIMessage(MessageRole.User, new TextMessageContent(text))));

				await CreateCompletionAndRespondAsync(message.Chat, chat, ct);
			}
			finally
			{
				state.OperationSync.Release();
			}
		}

		private async Task Start(TGMessage message, CancellationToken ct)
		{
			_users.TryRemove(message.Chat.Id, out _);
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Bot started successfully", cancellationToken: ct);
		}

		private async Task Restart(TGMessage message, CancellationToken ct)
		{
			GetState(message.Chat.Id).Chat.ModifyMessages(s => s.Clear());
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Bot restarted successfully", cancellationToken: ct);
		}

		private async Task Settings(TGMessage message, CancellationToken ct)
		{
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Settings list:", replyMarkup: _settingsKeyboardMarkup, cancellationToken: ct);
		}

		public async override Task ProcessUserCallbackAsync(CallbackQuery callback, CancellationToken ct)
		{
			var data = callback.Data;
			if (data is not null && data.StartsWith('S'))
			{
				var state = GetState(callback.From.Id);
				state.ActiveParameterToChange = data[1..];
				await Client.NativeClient.SendTextMessageAsync(callback.From.Id, "Please enter new value for paramter", cancellationToken: ct);
			}
		}

		private UserState GetState(long userId)
		{
			return _users.GetOrAdd(userId, (userID) => new UserState(_aiClient.CreateChat()));
		}

		private async Task CreateCompletionAndRespondAsync(ChatId tgChat, IChat chat, CancellationToken ct)
		{
			var preMessage = await Client.NativeClient.SendTextMessageAsync(tgChat, "Generating, please wait", cancellationToken: ct);

			using (var holder = _userDataRepository.Get<UserSettings>(UserSettings.StorageId))
			{
				chat.Options = holder.Object.Options;
			}

			var task = chat.CreateChatCompletionAsync();

			await Task.WhenAll(Task.Delay(2000, ct), task);

			var response = task.Result;
			chat.ModifyMessages(s => s.Add(response));

			await Client.NativeClient.EditMessageTextAsync(tgChat, preMessage.MessageId, response.Content.PresentAsString(), cancellationToken: ct, parseMode: ParseMode.Markdown);
		}


		private class UserState
		{
			public UserState(IChat chat)
			{
				Chat = chat;
			}


			public string? ActiveParameterToChange { get; set; }

			public SemaphoreSlim OperationSync { get; } = new SemaphoreSlim(1);

			public IChat Chat { get; }
		}
	}
}