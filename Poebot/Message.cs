namespace Poebot
{
    public class LoadedPhoto
    {
        public readonly long VkId;
        public readonly long VkOwnerId;
        public readonly string TelegramId;

        public LoadedPhoto(long vkId = 0, long vkOwnerId = 0, string telegramId = null)
        {
            VkId = vkId;
            VkOwnerId = vkOwnerId;
            TelegramId = telegramId;
        }
    }

    public class Message
    {
        public readonly string Text;
        public readonly byte[] Image;
        public readonly LoadedPhoto Loaded_Photo;
        public readonly string SysInfo;

        public Message(string text = null, byte[] image = null, LoadedPhoto loadedPhoto = null, string sysInfo = "")
        {
            Text = text;
            Image = image;
            Loaded_Photo = loadedPhoto;
            SysInfo = sysInfo;
        }
    }
}
