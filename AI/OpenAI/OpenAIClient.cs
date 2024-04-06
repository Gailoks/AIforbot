using System.Text.Json;
using System.Text;
using TelegramAIBot.AI.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TelegramAIBot.AI.OpenAI
{
	internal sealed class OpenAIClient : IAIClient
	{
		private readonly Configuration _configuration;
		private readonly HttpClient _httpClient = new();
		private readonly SemaphoreSlim _requestSync = new(3, 3);


		public OpenAIClient(IOptions<Configuration> configuration)
		{
			_configuration = configuration.Value;
			_requestSync = new SemaphoreSlim(configuration.Value.RequestConcurrentLimit);
		}


		public async Task<ServerResponse<TResponse>> SendMessageAsync<TResponse>(string endpoint, object body, HttpMethod method, Dictionary<string, string>? headers = null)
			where TResponse : notnull
		{
			headers ??= [];
			headers.Add("Authorization", "Bearer " + _configuration.Token);

			var serializedBody = JsonConvert.SerializeObject(body, new JsonSerializerSettings{NullValueHandling = NullValueHandling.Ignore});
			var content = new StringContent(serializedBody, Encoding.UTF8, "application/json");

			var request = new HttpRequestMessage()
			{
				Content = content,
				Method = method,
				RequestUri = new Uri(_configuration.OpenAIServer + endpoint)
			};

			foreach (var header in headers)
				request.Headers.Add(header.Key, header.Value);

			await _requestSync.WaitAsync();
			HttpResponseMessage response;
			try
			{
				response = await _httpClient.SendAsync(request);
			}
			finally { _requestSync.Release(); }

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

		public async Task<TextEmbedding> CreateEmbeddingAsync(string model, string text)
		{
			var request = new
			{
				model = model,
				input = text
			};

			var response = await SendMessageAsync<JObject>("v1/embeddings", request, HttpMethod.Post);

			dynamic body = response.ResponseBody;
			var embedding = (float[])body.data[0].embedding.ToObject<float[]>();

			return new TextEmbedding(embedding);
		}


		public class Configuration
		{
			public string OpenAIServer { get; init; } = "https://api.openai.com/";

			public int RequestConcurrentLimit { get; init; } = 3;

			public required string Token { get; init; }
		}

		public record ServerResponse<TResponse>(TResponse ResponseBody, HttpResponseMessage RawServerResponse) where TResponse : notnull;
	}
}
