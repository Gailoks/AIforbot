/// this is chat class
namespace TelegramBot
{
    class Chat
    {
        private string model = "mistral";
        private float top_p,temp;
        private Message[] messages;

        public async Task<string> Ask(string message)
        {
            return "sldkjfldksj";
        }
    }
    internal enum MessageRole
    {
        System,
        User,
        Assistant
    }

    internal abstract class MessageContent
    {

    }

    internal record class Message(MessageRole Role, MessageContent Content);
}
