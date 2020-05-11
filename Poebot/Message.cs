namespace Bot
{
    public class LoadedPhoto
    {
        public long VkId { get; }
        public long VkOwnerId { get; }
        public string TelegramId { get; }

        public LoadedPhoto(long vkId = 0, long vkOwnerId = 0, string telegramId = null)
        {
            VkId = vkId;
            VkOwnerId = vkOwnerId;
            TelegramId = telegramId;
        }
    }

    public class Message
    {
        public string Text { get; }
        private readonly byte[] image;
        public LoadedPhoto LoadedPhoto { get; }
        public string SysInfo { get; }

        public Message(string text = null, byte[] image = null, LoadedPhoto loadedPhoto = null, string sysInfo = "")
        {
            Text = text;
            this.image = image;
            LoadedPhoto = loadedPhoto;
            SysInfo = sysInfo;
        }

        public byte[] Image()
        {
            return image;
        }

        public bool DoesHaveAnImage() => image != null;
    }
}
