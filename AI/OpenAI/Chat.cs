using System.Text.Json.Serialization;
using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.AI.OpenAI
{
	internal class Chat : AbstractChat
	{
		private readonly OpenAIClient _client;


		public Chat(OpenAIClient client)
		{
			_client = client;
		}


		protected override async Task<Message> CreateChatCompletionAsyncInternal()
		{
			ChatCompletionOptions options;
			ApiMessage[] apiMessages;

			options = Options;
			apiMessages = Messages
				.Select(ApiMessage.FromMessage)
				.Prepend(new ApiMessage { Role = "system", Content = options.SystemPrompt ?? "You are useful assistant" })
				.ToArray();

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

			return message;
		}


#pragma warning disable CS0649
		private class ChatCompletionRequestBody
		{
			[JsonPropertyName("model")] public string ModelName { get; init; } = "gpt-3.5-turbo";

			[JsonPropertyName("top_p")] public double TopP { get; init; } = 1.0;

			[JsonPropertyName("temperature")] public double Temperature { get; init; } = 1.0;

			[JsonPropertyName("frequency_penalty")] public double FrequencyPenalty { get; init; } = 0.0;

			[JsonPropertyName("messages")] public required ApiMessage[] Messages { get; init; }
		}

		private class ChatCompletionResponseBody
		{
			[JsonPropertyName("choices")] public required ApiCompletionChoice[] Choices { get; init; }
		}

		private class ApiCompletionChoice
		{
			[JsonPropertyName("index")] public int Index { get; init; }

			[JsonPropertyName("message")] public required ApiMessage Message { get; init; }
		}

		private class ApiMessage
		{
			[JsonPropertyName("role")] public string Role { get; init; } = string.Empty;

			[JsonPropertyName("content")] public string Content { get; init; } = string.Empty;


			public static ApiMessage FromMessage(Message message)
			{
				if (message.Content.IsPresentableAsString)
				{
					return new ApiMessage { Role = message.Role.ToString().ToLower(), Content = message.Content.PresentAsString() };
				}
				else throw new NotSupportedException("Enable to work with non PresentableAsString message content");
			}
		}
#pragma warning restore CS0649
	}
}
