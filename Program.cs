using Microsoft.Extensions.Configuration;
using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.AI.Gug;
using TelegramAIBot.AI.OpenAI;
using TelegramAIBot.Telegram;
using TomLonghurst.ReadableTimeSpan;

namespace TelegramAIBot
{
	class Program
	{
		public static void Main(string[] args)
		{
			ReadableTimeSpan.EnableConfigurationBinding();

			var config = new ConfigurationBuilder().AddJsonFile("config.json").Build();


			IAIClient aiClient;

		tryAgain:
			Console.Write("You wanna use real openAI client or gug? [gug|real]: ");
			var ans = Console.ReadLine();

			if (ans == "gug")
			{
				GugClient.Configuration configuration = config.GetSection("AI:Gug").Get<GugClient.Configuration>()
					?? throw new Exception("No configuration found for gug client. Fix it in config.json file [AI:Gug]");

				aiClient = new GugClient(configuration);

			}
			else if (ans == "real")
			{
				OpenAIClient.Configuration configuration = config.GetSection("AI:OpenAI").Get<OpenAIClient.Configuration>()
					?? throw new Exception("No configuration found for openAI client. Fix it in config.json file [AI:OpenAI]");

				aiClient = new OpenAIClient(configuration);
			}
			else goto tryAgain;



			var module = new TelegramModule(aiClient);


			TelegramClient.Configuration tgConfiguration = config.GetSection("Telegram").Get<TelegramClient.Configuration>()
					?? throw new Exception("No configuration found for telegram client. Fix it in config.json file [Telegram]");

			var telegramClient = new TelegramClient(tgConfiguration, module);

			telegramClient.Start();

			Thread.Sleep(-1);
		}
	}
}