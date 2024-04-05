using System.Collections.Immutable;
using TelegramAIBot.AI.Abstractions;

namespace TelegramAIBot.RAG
{
	internal sealed class RAGChatDecorator : AbstractChat
	{
		private readonly RAGContext _context;
		private readonly IChat _chat;


		public RAGChatDecorator(RAGContext context, IChat chat) : base(chat.Id)
		{
			_context = context;
			_chat = chat;
		}


		protected override async Task<Message> CreateChatCompletionAsyncInternal()
		{
			var lastMessage = Messages.Last();
			var visitor = new MessageVisitor();
			var prompt = lastMessage.Content.Visit(visitor);
			var chunk = await _context.AssociateChunkAsync(prompt);
			var rag = new RAGMessageContent(chunk, prompt);
			var message = new Message(lastMessage.Role, rag);

			ModifyMessages(s => s.Replace(lastMessage, message, null));

			_chat.Options = Options;
			_chat.Messages = Messages.Select(s => s.Content is RAGMessageContent rag ? s with { Content = rag.AsTextContent() } : s).ToImmutableArray();

			return await _chat.CreateChatCompletionAsync();		
		}


		private class MessageVisitor : IMessageContentVisitor<string>
		{
			public string VisitText(TextMessageContent textContent)
			{
				return textContent.Text;
			}
		}
		private class RAGMessageContent : MessageContent
		{
			public RAGMessageContent(TextChunk chunk, string prompt)
			{
				Chunk = chunk;
				Prompt = prompt;
			}

			public TextChunk Chunk { get; }

			public string Prompt { get; }

			public override bool IsPresentableAsString => false;

			public TextMessageContent AsTextContent() => new(Chunk.Text + Prompt);

			public override TResult Visit<TResult>(IMessageContentVisitor<TResult> visitor)
			{
				throw new NotSupportedException();
			}
		}
	}
}