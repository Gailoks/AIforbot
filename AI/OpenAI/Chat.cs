using Newtonsoft.Json.Linq;
using System.Data;
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
			var messages = Messages;
			var apiMessages = messages
				.Select(msg =>
				{
					return new
					{
						content = msg.Content.Visit(visitor),
						role = msg.Role.ToString().ToLower()
					};
				})
				.Prepend(new { content = (object)(options.SystemPrompt ?? "You are useful assistant"), role = "system" })
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

			var response = await _client.SendMessageAsync<JObject>("v1/chat/completions", request, HttpMethod.Post);

			dynamic choice = response.ResponseBody;
			var content = choice.choices[0].message.content.ToObject<string>();

			var message = new Message(MessageRole.Assistant, new TextMessageContent(content));

			return message;
		}


		private class ContentVisitor : IMessageContentVisitor<object>
		{
			public object VisitText(TextMessageContent textContent)
			{
				return textContent.Text;
			}
		}
	}
}
