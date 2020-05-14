using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Bot
{
    public class TestPhoto : AbstractPhoto
    {
        public TestPhoto(string cachePath = null, long id = 0) : base(cachePath, id)
        {
        }

        public override bool SavePhoto(string name, byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var bmp = new Bitmap(stream);
                bmp.Save("image.png", ImageFormat.Png);
                bmp.Dispose();
            }

            return true;
        }

        public override bool UploadPhoto(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var bmp = new Bitmap(stream);
                bmp.Save("image.png", ImageFormat.Png);
                bmp.Dispose();
            }

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
            return null;
        }
    }
}