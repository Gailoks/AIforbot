using Microsoft.Extensions.Configuration;
using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.AI.Gug;
using TelegramAIBot.AI.OpenAI;
using TomLonghurst.ReadableTimeSpan;

namespace TelegramAIBot
{
	class Program
	{
		public static async Task Main(string[] args)
		{
			ReadableTimeSpan.EnableConfigurationBinding();

			var config = new ConfigurationBuilder().AddJsonFile("config.json").Build();


			IAIClient client;

		tryAgain:
			Console.Write("You wanna use real openAI client or gug? [gug|real]: ");
			var ans = Console.ReadLine();

			if (ans == "gug")
			{
				GugClient.Configuration configuration = config.GetSection("AI:Gug").Get<GugClient.Configuration>()
					?? throw new Exception("No configuration found for gug client. Fix it in config.json file [AI:Gug]");

				client = new GugClient(configuration);

			}
			else if (ans == "real")
			{
				OpenAIClient.Configuration configuration = config.GetSection("AI:OpenAI").Get<OpenAIClient.Configuration>()
					?? throw new Exception("No configuration found for openAI client. Fix it in config.json file [AI:OpenAI]");

				client = new OpenAIClient(configuration);
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