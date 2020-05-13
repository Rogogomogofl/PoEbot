namespace Bot
{
    public abstract class AbstractPhoto
    {
        public readonly string CachePath;
        public readonly long Id;
        protected string[] _content;
        public AbstractPhoto(string cachePath, long id)
        {
            CachePath = cachePath;
            Id = id;
            _content = null;
        }

        public abstract bool SavePhoto(string name, byte[] bytes);
        public abstract bool UploadPhoto(byte[] bytes);
        public abstract bool LoadPhotoFromFile(string name);
        public abstract bool GetPresetPhoto(string name);
        public abstract string[] GetContent();
    }
}