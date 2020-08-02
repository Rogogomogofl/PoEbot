using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using BotHandlers.Abstracts;

namespace BotHandlers.Mocks
{
    public class MockPhoto : AbstractPhoto
    {
        private readonly bool _saveBytes;
        public MockPhoto(bool saveBytes, string cachePath = null, long id = 0) : base(cachePath, id)
        {
            _saveBytes = saveBytes;
        }

        public override bool SavePhoto(string name, byte[] bytes)
        {
            if (bytes == null)
            {
                return false;
            }

            if (_saveBytes)
            {
                using var stream = new MemoryStream(bytes);
                var bmp = new Bitmap(stream);
                bmp.Save("image.png", ImageFormat.Png);
                bmp.Dispose();
            }

            Content = new string[1];

            return true;
        }

        public override bool UploadPhoto(byte[] bytes)
        {
            if (bytes == null)
            {
                return false;
            }

            if (_saveBytes)
            {
                using var stream = new MemoryStream(bytes);
                var bmp = new Bitmap(stream);
                bmp.Save("image.png", ImageFormat.Png);
                bmp.Dispose();
            }

            Content = new string[1];

            return true;
        }

        public override bool LoadPhotoFromFile(string name)
        {
            return false;
        }

        public override bool GetPresetPhoto(string name)
        {
            return false;
        }

        public override string[] GetContent()
        {
            return Content;
        }
    }
}