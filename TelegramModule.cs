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
				await ProcessParametersInput(message.Chat, text, state, ct);
				state.ActiveParameterToChange = null;
				return;
			}

			var chat = state.Chat;

			chat.ModifyMessages(s => s.Add(new AIMessage(MessageRole.User, new TextMessageContent(text))));

			await CreateCompletionAndRespondAsync(message.Chat, chat, ct);
		}

		private async Task ProcessParametersInput(ChatId chat, string text, UserState state, CancellationToken ct)
		{
			using var holder = _userDataRepository.Get<UserSettings>(UserSettings.StorageId);
			var oldValue = holder.Object;

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

				await Client.NativeClient.SendTextMessageAsync(chat, $"{state.ActiveParameterToChange} has been successfully changed", cancellationToken: ct);
			}
			catch (Exception)
			{
				await Client.NativeClient.SendTextMessageAsync(chat, $"Invalid parameter value for {state.ActiveParameterToChange}", cancellationToken: ct);
				holder.Object = oldValue;
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
			using var holder = _userDataRepository.Get<UserSettings>(UserSettings.StorageId);
			var Options = holder.Object.Options;
			await Client.NativeClient.SendTextMessageAsync(message.Chat, 
			$"""
			Settings:
			Temperature - {Options.Temperature?.ToString() ?? "Default"}
			Top-p - {Options.TopP?.ToString() ?? "Default"}
			Frequency penalty - {Options.FrequencyPenalty?.ToString() ?? "Default"}
			Model name - {Options.ModelName}
			System prompt - {Options.SystemPrompt ?? "None"}
			""", replyMarkup: _settingsKeyboardMarkup, cancellationToken: ct);
		}

		public async override Task ProcessUserCallbackAsync(CallbackQuery callback, CancellationToken ct)
		{
			var data = callback.Data;
			if (data is not null && data.StartsWith('S'))
			{
				var state = GetState(callback.From.Id);
				state.ActiveParameterToChange = data[1..];
				await Client.NativeClient.SendTextMessageAsync(callback.From.Id, $"Please enter new value for parameter: {data[1..]}", cancellationToken: ct);
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

			public IChat Chat { get; }
		}
	}
}