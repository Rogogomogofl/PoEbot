namespace Bot
{
    public class Message
    {
        public string Text { get; }

        public readonly AbstractPhoto Photo;

        public Message(string text = null, AbstractPhoto photo = null)
        {
            Text = text;
            Photo = photo;
        }
    }
}