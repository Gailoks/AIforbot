using System.Data;
using System.Dynamic;
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
					var apiMessageContent = msg.Contents.Select(s => s.Visit(visitor));

					return new
					{
						content = apiMessageContent,
						role = msg.Role.ToString().ToLower()
					};
				})
				.Prepend(new { content = (IEnumerable<object>)new object[] { options.SystemPrompt ?? "You are useful assistant" }, role = "system" })
				.ToArray();

			var request =
			new
			{
				frequencyPenalty = options.FrequencyPenalty,
				top_p = options.TopP,
				temperature = options.Temperature,
				model = options.ModelName,

				messages = apiMessages
			};

			var response = await _client.SendMessageAsync<ExpandoObject>("v1/chat/completions", request, HttpMethod.Post);

			dynamic choice = response.ResponseBody;
			var content = choice.choices[0].message.content;

			var message = new Message(MessageRole.Assistant, new TextMessageContent(content));

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
	}
}
