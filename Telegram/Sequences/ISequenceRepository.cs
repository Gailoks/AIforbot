namespace TelegramAIBot.Telegram.Sequences;

interface ISequenceRepository
{
    public void Load();

    public IEnumerable<TelegramSequence> List();
}
