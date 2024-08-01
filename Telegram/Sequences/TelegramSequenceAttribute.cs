namespace TelegramAIBot.Telegram.Sequences;

class TelegramSequenceAttribute(SequenceTrigger trigger) : Attribute
{
    public SequenceTrigger Trigger { get; } = trigger;
}
