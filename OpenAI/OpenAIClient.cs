using System.Net.Http.Json;
using System.Text.Json;

namespace TelegramAIBot.OpenAI
{
	internal sealed class OpenAIClient
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
			headers ??= new();
			headers.Add("Authorization", _configuration.Token);
			headers.Add("Content-Type", "application/json");

			var content = JsonContent.Create(body);

			var request = new HttpRequestMessage()
			{
				Content = content,
				Method = method,
				RequestUri = new Uri(_configuration + endpoint)
			};

			foreach (var header in headers)
				request.Headers.Add(header.Key, header.Value);

			var response = await _httpClient.SendAsync(request);
			var responseContent = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode == false)
			{
				throw new OpenAIApiException(endpoint, request, body, response, responseContent);
			}

			var responseAsObject = JsonSerializer.Deserialize<TResponse>(responseContent) ?? throw new NullReferenceException();

			return new ServerResponse<TResponse>(responseAsObject, response);
		}

		public Chat CreateChat()
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
