namespace TelegramAIBot.OpenAI
{
    class Chat()
    {
        private string _model = "mistral";

        private float _topP = 0.9f, _temp = 0.8f;
        private string _systemPrompt = "Ты полезный ассистент помощник";
        private Message[] _messages = [];
        private Status _status = Status.None;
        public async Task<string> Ask(string message)
        {
            return "To do";
        }
        public Status GetStatus{get{return _status;}}
        public void Clear() => _messages = [];
    }
}
