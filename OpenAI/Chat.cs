using FluentValidation;
using System.Text.Json.Serialization;

namespace TelegramAIBot.OpenAI
{
	internal class Chat
	{
		private readonly OpenAIClient _client;
		private readonly IValidator<ChatCompletionOptions> _optionsValidator;
		private readonly List<Message> _messages = [];
		private readonly object _syncRoot = new();
		private ChatCompletionOptions _options;


		public Chat(OpenAIClient client)
		{
			_client = client;
			_optionsValidator = new ChatCompletionOptionsValidator();
			_options = new ChatCompletionOptions();
		}


		public ChatCompletionOptions Options { get => _options; set { lock (_syncRoot) { _optionsValidator.ValidateAndThrow(value); _options = value; } } }


		public void ModifyOptions(Func<ChatCompletionOptions, ChatCompletionOptions> modification)
		{
			lock (_syncRoot)
			{
				Options = modification(Options);
			}
		}

		public void AddMessage(Message message)
		{
			lock (_syncRoot)
			{
				_messages.Add(message);
			}
		}

		public async Task<Message> CreateChatCompletionAsync(bool autoAddToChat = true)
		{
			ChatCompletionOptions options;
			ApiMessage[] apiMessages;

			lock (_syncRoot)
			{
				options = Options;
				apiMessages = _messages
					.Select(ApiMessage.FromMessage)
					.Prepend(new ApiMessage { Role = "system", Content = options.SystemPrompt })
					.ToArray();
			}

			var request = new ChatCompletionRequestBody
			{
				FrequencyPenalty = options.FrequencyPenalty,
				TopP = options.TopP,
				Temperature = options.Temperature,
				ModelName = options.ModelName,

				Messages = apiMessages
			};

			var response = await _client.SendMessageAsync<ChatCompletionResponseBody>("v1/chat/completions", request, HttpMethod.Post);

			var choice = response.ResponseBody.Choices.FirstOrDefault() ?? throw new Exception("No choices provided by OpenAI server");

			var apiMessage = choice.Message;

			var message = new Message(MessageRole.Assistant, new TextMessageContent(apiMessage.Content));

			if (autoAddToChat) AddMessage(message);

			return message;
		}


#nullable disable
#pragma warning disable CS0649
		private class ChatCompletionRequestBody
		{
			[JsonPropertyName("model")] public string ModelName = "gpt-3.5-turbo";
			[JsonPropertyName("top_p")] public double TopP = 1.0;
			[JsonPropertyName("temperature")] public double Temperature = 1.0;
			[JsonPropertyName("frequency_penalty")] public double FrequencyPenalty = 0.0;

			[JsonPropertyName("messages")] public ApiMessage[] Messages;
		}

		private class ChatCompletionResponseBody
		{
			[JsonPropertyName("choices")] public ApiCompletionChoice[] Choices;
		}

		private class ApiCompletionChoice
		{
			[JsonPropertyName("index")] public int Index;
			[JsonPropertyName("message")] public ApiMessage Message;
		}

		private class ApiMessage
		{
			[JsonPropertyName("role")] public string Role = string.Empty;
			[JsonPropertyName("content")] public string Content = string.Empty;


			public static ApiMessage FromMessage(Message message)
			{
				if (message.Content.IsPresentableAsString)
				{
					return new ApiMessage { Role = message.Role.ToString(), Content = message.Content.PresentAsString() };
				}
				else throw new NotSupportedException("Enable to work with non PresentableAsString message content");
			}
		}
#pragma warning restore CS0649
#nullable restore
	}
}
