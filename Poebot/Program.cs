using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Poebot
{
    class Program
    {
        static void Main(string[] args)
        {
            Poewatch poewatch = new Poewatch();
            while (true)
            {
                Poebot poebot = new Poebot(poewatch);
                string query = Console.ReadLine();
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Message message = poebot.ProcessRequest(query);
                sw.Stop();
                Console.WriteLine(message.Text);
                if (message.Image != null)
                    using (MemoryStream stream = new MemoryStream(message.Image))
                    {
                        Bitmap bmp = new Bitmap(stream); 
                        bmp.Save("image.png", ImageFormat.Png);
                        bmp.Dispose();
                    }
            }
        }
    }
}
