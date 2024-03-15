using TelegramAIBot.OpenAI;

namespace TelegramAIBot
{
	class Program
	{
		public static async Task Main(string[] args)
		{
			var openAIClient = new OpenAIClient(new OpenAIClient.Configuration
			{
				Token = ""
			});

			var chat = openAIClient.CreateChat();

			chat.Options = new(
				ModelName: "gpt-3.5-turbo",
				SystemPrompt: "You are useful assistance"
			);



			chat.AddMessage(new Message(MessageRole.User, new TextMessageContent("Hello assistance, who are you")));

			while (true)
			{
				var userInput = Console.ReadLine() ?? string.Empty;

				chat.AddMessage(new Message(MessageRole.User, new TextMessageContent(userInput)));


				var response = await chat.CreateChatCompletionAsync();

				if (response.Content.IsPresentableAsString)
					Console.WriteLine(response.Content.PresentAsString());
				else
					Console.WriteLine("Content cannot be represented as string");
			}
		}
	}
}