namespace Bot
{
    public class Message
    {
        public string Text { get; }

        public AbstractPhoto Photo;

        public string SysInfo { get; }

        public Message(string text = null, AbstractPhoto photo = null, string sysInfo = "")
        {
            Text = text;
            Photo = photo;
            SysInfo = sysInfo;
        }
    }
}