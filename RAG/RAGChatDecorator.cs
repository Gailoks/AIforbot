using System.Collections.Immutable;
using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.RAG
{
	internal sealed class RAGChatDecorator : AbstractChat
	{
		private readonly Dictionary<Message, RAGMessageContent> _substitutionDictionary = [];
		private readonly RAGContext _context;
		private readonly IChat _chat;


		public RAGChatDecorator(RAGContext context, IChat chat) : base(chat.Id)
		{
			_context = context;
			_chat = chat;

			Options = chat.Options;
			Messages = chat.Messages;

			OptionsChanged += (_) => { _chat.Options = Options; };
			MessagesChanged += (_) => ApplyMessagesToBase();
		}


		protected override async Task<Message> CreateChatCompletionAsyncInternal()
		{
			var messages = Messages;

			await CreateSubstitutionWithRAGAsync(messages[messages.Count - 1]);

			ApplyMessagesToBase();

			//Clear _substitutionDictionary
			foreach (var key in _substitutionDictionary.Keys)
				if (messages.Contains(key) == false)
					_substitutionDictionary.Remove(key);

			return await _chat.CreateChatCompletionAsync();
		}

		private void ApplyMessagesToBase()
		{
			_chat.Messages = Messages.Select(TrySubstitute).ToImmutableArray();
		}

		private async Task CreateSubstitutionWithRAGAsync(Message message)
		{
			var visitor = new MessageVisitor();
			var prompt = message.Content.Visit(visitor);
			var chunk = await _context.AssociateChunkAsync(prompt);
			var rag = new RAGMessageContent(chunk, prompt);

			_substitutionDictionary.Add(message, rag);
		}

		private Message TrySubstitute(Message message)
		{
			if (_substitutionDictionary.TryGetValue(message, out var content))
				return message with { Content = content };
			else return message;
		}


		private class MessageVisitor : IMessageContentVisitor<string>
		{
			public string VisitText(TextMessageContent textContent)
			{
				return textContent.Text;
			}
		}

		private class RAGMessageContent : TextMessageContent
		{
			public RAGMessageContent(TextChunk chunk, string prompt)
				: base(FormText(chunk, prompt))
			{
				Chunk = chunk;
				Prompt = prompt;
			}


			public TextChunk Chunk { get; }

			public string Prompt { get; }


			private static string FormText(TextChunk chunk, string prompt)
			{
				return chunk.Text + "\n\n" + prompt;
			}
		}
	}
}