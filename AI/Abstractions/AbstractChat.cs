using FluentValidation;
using System.Collections.Immutable;

namespace TelegramAIBot.AI.Abstractions
{
	internal abstract class AbstractChat : IChat
	{
		private readonly IValidator<ChatCompletionOptions> _optionsValidator;
		private readonly object _syncRoot = new();
		private readonly SemaphoreSlim _asyncOpSync = new(1);
		private ChatCompletionOptions _options;
		private IImmutableList<Message> _messages;


		protected AbstractChat(Guid id)
		{
			_optionsValidator = new ChatCompletionOptionsValidator();
			_options = new ChatCompletionOptions();
			_messages = ImmutableList<Message>.Empty;
			Id = id;
		}


		public IImmutableList<Message> Messages
		{
			get => _messages;
			set
			{
				lock (_syncRoot)
				{
					var oldValue = _messages;
					_messages = value;
					MessagesChanged?.Invoke(oldValue);
				}
			}
		}

		public ChatCompletionOptions Options
		{
			get => _options;
			set
			{
				lock (_syncRoot)
				{
					_optionsValidator.ValidateAndThrow(value);
					var oldValue = _options;
					_options = value;
					OptionsChanged?.Invoke(oldValue);
				}
			}
		}

		public Guid Id { get; }


		public event Action<IImmutableList<Message>>? MessagesChanged;
		public event Action<ChatCompletionOptions>? OptionsChanged;


		public void ModifyOptions(Func<ChatCompletionOptions, ChatCompletionOptions> modification)
		{
			lock (_syncRoot)
			{
				Options = modification(Options);
			}
		}

		public void ModifyMessages(Func<IImmutableList<Message>, IImmutableList<Message>> modification)
		{
			lock (_syncRoot)
			{
				Messages = modification(Messages);
			}
		}

		public async Task<Message> CreateChatCompletionAsync()
		{
			await _asyncOpSync.WaitAsync();

			Message result;

			try
			{
				result = await CreateChatCompletionAsyncInternal();
			}
			finally
			{
				_asyncOpSync.Release();
			}

			return result;
		}

		protected abstract Task<Message> CreateChatCompletionAsyncInternal();
	}
}
