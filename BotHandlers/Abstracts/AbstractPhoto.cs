namespace BotHandlers.Abstracts
{
    public abstract class AbstractPhoto
    {
        protected readonly string CachePath;
        protected readonly long Id;
        protected string[] Content;

        protected AbstractPhoto(string cachePath, long id)
        {
            CachePath = cachePath;
            Id = id;
        }

        public abstract bool SavePhoto(string name, byte[] bytes);
        public abstract bool UploadPhoto(byte[] bytes);
        public abstract bool LoadPhotoFromFile(string name);
        public abstract bool GetPresetPhoto(string name);
        public abstract string[] GetContent();
    }
}