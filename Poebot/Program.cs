using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;

namespace Poebot
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Poewatch poewatch = new Poewatch();
            Console.WriteLine("Done");
            while (true)
            {
                Poebot poebot = new Poebot(poewatch);
                string query = Console.ReadLine();
                if (query == string.Empty) continue;
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
                Console.WriteLine($"Время обработки запроса: {sw.ElapsedMilliseconds} мс");
            }
        }
    }
}
