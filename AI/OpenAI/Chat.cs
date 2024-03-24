using System.Data;
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
			ChatCompletionOptions options = Options;

			var visitor = new ContentVisitor();
			var apiMessages = Messages
				.Select(msg =>
				{
					return new
					{
						content = msg.Content.PresentAsString(),
						role = msg.Role.ToString().ToLower()
					};
				})
				.Prepend(new { content = options.SystemPrompt ?? "You are useful assistant" , role = "system" })
				.ToArray();

			var request =
			new
			{
				repeat_penalty = options.FrequencyPenalty,
				top_p = options.TopP,
				temperature = options.Temperature,
				model = options.ModelName,

				messages = apiMessages
			};

			var response = await _client.SendMessageAsync<ChatCompletionResponseBody>("v1/chat/completions", request, HttpMethod.Post);

			var choice = response.ResponseBody.Choices.FirstOrDefault() ?? throw new Exception("No choices provided by OpenAI server");			
			var message = new Message(MessageRole.Assistant, new TextMessageContent(choice.Message.Content));
			return message;
		}


		private class ContentVisitor : IMessageContentVisitor<object>
		{
			public object VisitImage(ImageMessageContent imageContent)
			{
				return new
				{
					type = "image_url",
					image_url = new
					{
						url = imageContent.Url
					}
				};
			}

			public object VisitText(TextMessageContent textContent)
			{
				return new
				{
					type = "text",
					text = textContent.Text
				};
			}
		}
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


		}
	}
}
