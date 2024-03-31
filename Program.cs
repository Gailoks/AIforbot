using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramAIBot.AI.Abstractions;
using TelegramAIBot.AI.Gug;
using TelegramAIBot.AI.OpenAI;
using TelegramAIBot.Telegram;
using TelegramAIBot.UserData;
using TelegramAIBot.UserData.InMemory;
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


			if (args.Contains("--useGugClient"))
			{
				Console.WriteLine("Using gug client");
				GugClient.Configuration configuration = config.GetSection("AI:Gug").Get<GugClient.Configuration>()
					?? throw new Exception("No configuration found for gug client. Fix it in config.json file [AI:Gug]");

				aiClient = new GugClient(configuration);
			}
			else
			{
				Console.WriteLine("Using real client");
				OpenAIClient.Configuration configuration = config.GetSection("AI:OpenAI").Get<OpenAIClient.Configuration>()
					?? throw new Exception("No configuration found for openAI client. Fix it in config.json file [AI:OpenAI]");

				aiClient = new OpenAIClient(configuration);
			}


			IUserDataRepository userDataRepository = new InMemoryUserDataRepository();

			var module = new TelegramModule(aiClient, userDataRepository);


			TelegramClient.Configuration tgConfiguration = config.GetSection("Telegram").Get<TelegramClient.Configuration>()
					?? throw new Exception("No configuration found for telegram client. Fix it in config.json file [Telegram]");

			var telegramClient = new TelegramClient(tgConfiguration, module);

			telegramClient.Start();

			Thread.Sleep(-1);
		}
	}
}