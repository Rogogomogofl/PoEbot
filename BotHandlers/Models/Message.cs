using BotHandlers.Abstracts;

namespace BotHandlers.Models
{
    public class Message
    {
        public string Text { get; }

        public readonly AbstractPhoto Photo;

        public Message(string text)
        {
            Text = text;
        }
        public Message(AbstractPhoto photo)
        {
            Photo = photo;
        }
        public Message(string text, AbstractPhoto photo)
        {
            Text = text;
            Photo = photo;
        }
    }
}