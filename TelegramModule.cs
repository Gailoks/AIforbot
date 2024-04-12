using System.Collections.Concurrent;
using Telegram.Bot;
using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.Telegram;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using TelegramAIBot.UserData;
using Telegram.Bot.Types.ReplyMarkups;
using FluentValidation;
using System.Globalization;
using TelegramAIBot.RAG;
using File = Telegram.Bot.Types.File;
using TGMessage = Telegram.Bot.Types.Message;
using AIMessage = TelegramAIBot.AI.Abstractions.Message;

namespace TelegramAIBot
{
	internal class TelegramModule : TelegramModuleBase
	{
		private readonly ConcurrentDictionary<long, UserState> _users = [];
		private readonly IAIClient _aiClient;
		private readonly IUserDataRepository _userDataRepository;
		private readonly TextExtractor _textExtractor;
		private readonly RAGProcessor _rag;
		private readonly IValidator<ChatCompletionOptions> _chatCompletionOptionsValidator = new ChatCompletionOptionsValidator();


		private readonly InlineKeyboardMarkup _settingsKeyboardMarkup = new(
		[
			[InlineKeyboardButton.WithCallbackData("Change system prompt", "Ssystem_prompt")],
			[InlineKeyboardButton.WithCallbackData("Change temp", "Stemperature")],
			[InlineKeyboardButton.WithCallbackData("Change top p", "Stop_p")],
			[InlineKeyboardButton.WithCallbackData("Change frequency penalty", "Sfrequency_penalty")]
		]);

		private readonly InlineKeyboardMarkup _ragCtrlKeyboardMarkup = new(
		[
			[InlineKeyboardButton.WithCallbackData("Disable RAG", "RAG_disable")],
			[InlineKeyboardButton.WithCallbackData("Suspend RAG", "RAG_suspend")],
			[InlineKeyboardButton.WithCallbackData("Resume RAG", "RAG_resume")],
		]);


		public TelegramModule(IAIClient aiClient, IUserDataRepository userDataRepository, TextExtractor textExtractor, RAGProcessor rag)
		{
			_aiClient = aiClient;
			_userDataRepository = userDataRepository;
			_textExtractor = textExtractor;
			_rag = rag;

			RegisterCommand("start", Start);
			RegisterCommand("restart", Restart);
			RegisterCommand("settings", Settings);
			RegisterCommand("rag", ControlRAG);
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
			using var holder = GetUserSettings(chat, out var so);

			try
			{
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
				holder.Object = so;
			}
		}

		private async Task Start(TGMessage message, CancellationToken ct)
		{
			_users.TryRemove(message.Chat.Id, out _);
			using var settings = GetUserSettings(message.Chat, out _);
			settings.Object = new();
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Bot started successfully", cancellationToken: ct);
		}

		private async Task Restart(TGMessage message, CancellationToken ct)
		{
			GetState(message.Chat.Id).Chat.ModifyMessages(s => s.Clear());
			await Client.NativeClient.SendTextMessageAsync(message.Chat, "Bot restarted successfully", cancellationToken: ct);
		}

		private async Task ControlRAG(TGMessage message, CancellationToken ct)
		{
			var state = GetState(message.Chat.Id);

			if (state.CurrentRAGState == UserState.RAGState.Disabled)
				await Client.NativeClient.SendTextMessageAsync(message.Chat, "RAG is disabled, to enable it send file to bot", cancellationToken: ct);
			else
				await Client.NativeClient.SendTextMessageAsync(message.Chat, $"RAG is {state.CurrentRAGState}", replyMarkup: _ragCtrlKeyboardMarkup, cancellationToken: ct);
		}

		private async Task Settings(TGMessage message, CancellationToken ct)
		{
			using (GetUserSettings(message.Chat, out var settings))
			{
				var options = settings.Options;
				await Client.NativeClient.SendTextMessageAsync(message.Chat, 
				$"""
				Settings:
				Temperature - {options.Temperature?.ToString() ?? "Default"}
				Top-p - {options.TopP?.ToString() ?? "Default"}
				Frequency penalty - {options.FrequencyPenalty?.ToString() ?? "Default"}
				Model name - {options.ModelName}
				System prompt - {options.SystemPrompt ?? "None"}
				""", replyMarkup: _settingsKeyboardMarkup, cancellationToken: ct);
			}
		}

		public async override Task ProcessUserCallbackAsync(CallbackQuery callback, CancellationToken ct)
		{
			var data = callback.Data;
			var state = GetState(callback.From.Id);

			if (data is not null)
			{
				if (data.StartsWith('S'))
				{
					state.ActiveParameterToChange = data[1..];
					await Client.NativeClient.SendTextMessageAsync(callback.From.Id, $"Please enter new value for parameter: {data[1..]}", cancellationToken: ct);
				}
				else
				{
					switch (data)
					{
						case "RAG_disable":
							state.ChangeRAGContext(null);
							await Client.NativeClient.SendTextMessageAsync(callback.From.Id, $"RAG disabled", cancellationToken: ct);
							break;

						case "RAG_suspend":
							state.SuspendRAG();
							await Client.NativeClient.SendTextMessageAsync(callback.From.Id, $"RAG suspended", cancellationToken: ct);
							break;

						case "RAG_resume":
							state.ResumeRAG();
							await Client.NativeClient.SendTextMessageAsync(callback.From.Id, $"RAG resumed", cancellationToken: ct);
							break;
					}
				}
			}
		}

		protected override async Task HandleDocumentAsync(TGMessage message, Document document, CancellationToken ct)
		{
			var stream = new MemoryStream();

			await Client.NativeClient.GetInfoAndDownloadFileAsync(document.FileId, stream, ct);

			var type = document.MimeType ?? "application/octet-stream";
			var rawData = stream.ToArray();

			var text = _textExtractor.Extract(rawData, type);
			UserState state = GetState(message.Chat.Id);

			var ragContext = await _rag.CreateContextAsync(text);

			state.ChangeRAGContext(ragContext);

			await Client.NativeClient.SendTextMessageAsync(message.Chat, $"RAG started successfully.\nHere is first text block:\n{ragContext.ToString()}", cancellationToken:ct);
		}

		private UserState GetState(long userId)
		{
			return _users.GetOrAdd(userId, (userID) => new UserState(_aiClient.CreateChat()));
		}

		private async Task CreateCompletionAndRespondAsync(ChatId tgChat, IChat chat, CancellationToken ct)
		{
			var preMessage = await Client.NativeClient.SendTextMessageAsync(tgChat, "Generating, please wait", cancellationToken: ct);

			using (GetUserSettings(tgChat, out var value))
			{
				chat.Options = value.Options;
			}

			var task = chat.CreateChatCompletionAsync();

			await Task.WhenAll(Task.Delay(2000, ct), task);

			var response = task.Result;
			chat.ModifyMessages(s => s.Add(response));

			await Client.NativeClient.EditMessageTextAsync(tgChat, preMessage.MessageId, response.Content.PresentAsString(), cancellationToken: ct, parseMode: ParseMode.Markdown);
		}

		private ObjectHolder<UserSettings> GetUserSettings(ChatId id, out UserSettings settings)
			=> GetUserSettings(id.Identifier ?? 0, out settings);

		private ObjectHolder<UserSettings> GetUserSettings(long id, out UserSettings settings)
		{
			var holder = _userDataRepository.Get<UserSettings>($"user_{id}");
			settings = holder.Object;
			return holder;
		}


		private class UserState
		{
			private readonly IChat _baseChat;


			public UserState(IChat baseChat)
			{
				_baseChat = baseChat;
				Chat = _baseChat;
			}


			public string? ActiveParameterToChange { get; set; }

			public RAGContext? RAGContext { get; private set; }

			public IChat Chat { get; private set; }

			public RAGState CurrentRAGState { get; private set; }


			public void ChangeRAGContext(RAGContext? context)
			{
				RAGContext = context;

				if (context is null)
				{
					Chat = _baseChat;
					CurrentRAGState = RAGState.Disabled;
				}
				else
				{
					Chat = new RAGChatDecorator(context, _baseChat);
					CurrentRAGState = RAGState.Enabled;
				}
			}

			public void SuspendRAG()
			{
				if (CurrentRAGState != RAGState.Enabled)
					throw new InvalidOperationException();

				CurrentRAGState = RAGState.Suspended;
				Chat = _baseChat;
			}

			public void ResumeRAG()
			{
				if (CurrentRAGState != RAGState.Suspended)
					throw new InvalidOperationException();

				CurrentRAGState = RAGState.Enabled;
				Chat = new RAGChatDecorator(RAGContext!, _baseChat);
			}


			public enum RAGState
			{
				Disabled = default,
				Suspended,
				Enabled
			}
		}
	}
}