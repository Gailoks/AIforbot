global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
		public static readonly EventId GugClientUsedLOG = new EventId(12, nameof(GugClientUsedLOG)).Form();


		public static void Main(string[] args)
		{
			ReadableTimeSpan.EnableConfigurationBinding();

			var config = new ConfigurationBuilder().AddJsonFile("config.json").Build();

			var serviceCollection = new ServiceCollection()
				.AddSingleton<IUserDataRepository, InMemoryUserDataRepository>()
				.AddSingleton<TelegramModule>()

				.AddLogging(sb => sb.AddConsole())

				.Configure<TelegramClient.Configuration>(config.GetSection("Telegram")) 
				.AddSingleton<TelegramClient>()
			;


			var isGugClientImplementationUsed = args.Contains("--useGugClient");
			if (isGugClientImplementationUsed)
			{
				Console.WriteLine("Using gug client");
				serviceCollection
					.Configure<GugClient.Configuration>(config.GetSection("AI:Gug"))
					.AddSingleton<GugClient>()
				;
			}
			else
			{
				Console.WriteLine("Using real client");
				serviceCollection
					.Configure<OpenAIClient.Configuration>(config.GetSection("AI:OpenAI"))
					.AddSingleton<OpenAIClient>()
				;
			}

			var services = serviceCollection.BuildServiceProvider();
			var logger = services.GetRequiredService<ILogger<Program>>();

			if (isGugClientImplementationUsed)
				logger.Log(LogLevel.Information, "Using gug version of ai client");

			services.GetRequiredService<TelegramClient>().Start();

			Thread.Sleep(-1);
		}
	}
}