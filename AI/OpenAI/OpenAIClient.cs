using System.Text.Json;
using System.Text;
using TelegramAIBot.AI.Abstractions;
using Newtonsoft.Json;

namespace TelegramAIBot.AI.OpenAI
{
	internal sealed class OpenAIClient : IAIClient
	{
		private readonly Configuration _configuration;
		private readonly HttpClient _httpClient = new();


		public OpenAIClient(Configuration configuration)
		{
			_configuration = configuration;
		}


		public async Task<ServerResponse<TResponse>> SendMessageAsync<TResponse>(string endpoint, object body, HttpMethod method, Dictionary<string, string>? headers = null)
			where TResponse : notnull
		{
			headers ??= [];
			headers.Add("Authorization", "Bearer " + _configuration.Token);

			var serializedBody = JsonConvert.SerializeObject(body);
			var content = new StringContent(serializedBody, Encoding.UTF8, "application/json");

			var request = new HttpRequestMessage()
			{
				Content = content,
				Method = method,
				RequestUri = new Uri(_configuration.OpenAIServer + endpoint)
			};

			foreach (var header in headers)
				request.Headers.Add(header.Key, header.Value);

			var response = await _httpClient.SendAsync(request);
			var responseContent = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode == false)
			{
				throw new OpenAIApiException(endpoint, request, body, response, responseContent);
			}

			var responseAsObject = JsonConvert.DeserializeObject<TResponse>(responseContent) ?? throw new NullReferenceException();

			return new ServerResponse<TResponse>(responseAsObject, response);
		}

		public IChat CreateChat()
		{
			return new Chat(this);
		}


		public class Configuration
		{
			public string OpenAIServer { get; init; } = "https://api.openai.com/";

			public required string Token { get; init; }
		}

		public record ServerResponse<TResponse>(TResponse ResponseBody, HttpResponseMessage RawServerResponse) where TResponse : notnull;
	}
}
