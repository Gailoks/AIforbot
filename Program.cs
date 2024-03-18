using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.AI.Gug;
using TelegramAIBot.AI.OpenAI;

namespace TelegramAIBot
{
	class Program
	{
		public static async Task Main(string[] args)
		{
			IAIClient client;

		tryAgain:
			Console.Write("You wanna use real openAI client or gug? [gug|real]: ");
			var ans = Console.ReadLine();

			if (ans == "gug")
			{
				client = new GugClient() { ChatCompletionCreationOperationDuration = TimeSpan.FromSeconds(4) };
			}
			else if (ans == "real")
			{
				client = new OpenAIClient(new OpenAIClient.Configuration
				{
					Token = "no-key",
					OpenAIServer = "http://192.168.2.106:8080/"

				});
			}
			else goto tryAgain;


			var chat = client.CreateChat();

			chat.Options = new(
				ModelName: "gpt-3.5-turbo",
				SystemPrompt: "You are useful assistance"
			);


			chat.ModifyMessages(s => s.Add(new Message(MessageRole.User, new TextMessageContent("Hello assistance, who are you"))));

			while (true)
			{
				var userInput = Console.ReadLine() ?? string.Empty;
				chat.ModifyMessages(s => s.Add(new Message(MessageRole.User, new TextMessageContent(userInput))));
				
				var response = await chat.CreateChatCompletionAsync();

				if (response.Content.IsPresentableAsString)
					Console.WriteLine(response.Content.PresentAsString());
				else
					Console.WriteLine("Content cannot be represented as string");
			}
		}
	}
}