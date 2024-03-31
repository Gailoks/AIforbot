using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramAIBot.Telegram
{
	internal abstract class TelegramModuleBase : ITelegramModule
	{
		private readonly Dictionary<string, Command> _commands = [];
		private TelegramClient? _client;


		protected TelegramClient Client => _client ?? throw new InvalidOperationException("Module has not been bound to client yet");


		public async virtual Task ProcessUserMessageAsync(Message message, CancellationToken ct)
		{
			switch(message.Type)
			{
				case MessageType.Text:
					var text = message.Text!;
					if (text.StartsWith(TelegramClient.CommandPrefix))
					{
						if (_commands.TryGetValue(text[1..], out var command))
						{
							await command.Handler(message, ct);
						}
						else
						{
							await HandleInvalidCommandAsync(message, ct);
						}
					}
					else
					{
						await HandleTextMessageAsync(message, message.Text!, ct);
					}

				break;

				case MessageType.Document:
					await HandleDocumentAsync(message, message.Document!, ct);
				break;

				case MessageType.Photo:
					await HandlePhotoAsync(message, message.Photo!, ct);
				break;
			}
		}


		protected virtual Task HandleTextMessageAsync(Message message, string text, CancellationToken ct) => Task.CompletedTask;

		protected virtual Task HandleDocumentAsync(Message message, Document document, CancellationToken ct) => Task.CompletedTask;

		protected virtual Task HandlePhotoAsync(Message message, PhotoSize[] photos, CancellationToken ct) => Task.CompletedTask;

		public virtual Task ProcessUserCallbackAsync(CallbackQuery callback, CancellationToken ct) => Task.CompletedTask;

		protected virtual async Task HandleInvalidCommandAsync(Message message, CancellationToken ct)
		{
			await Client.NativeClient.SendTextMessageAsync(message.Chat, $"Unknown command {message.Text}", cancellationToken: ct);
		}

		public void BindClient(TelegramClient client)
		{
			_client = client;
		}

		protected void RegisterCommand(Command command)
		{
			_commands.Add(command.Name, command);
		}

		protected void RegisterCommand(string name, CommandHandler handler) => RegisterCommand(new Command(name, handler));


		protected record class Command(string Name, CommandHandler Handler);


		protected delegate Task CommandHandler(Message message, CancellationToken ct);
	}
}