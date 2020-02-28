using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Timers;
using System.Xml;

namespace Bot
{
    class Program
    {
        static Timer rssUpdate;

        static void Main()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            rssUpdate = new Timer(10 * 1000);
            rssUpdate.Elapsed += UpdateRss;
            rssUpdate.AutoReset = true;
            //rssUpdate.Enabled = true;

            Poewatch poewatch = new Poewatch();
            Console.WriteLine("Working");
            while (true)
            {
                Poebot poebot = new Poebot(poewatch);
                string query = Console.ReadLine();
                if (string.IsNullOrEmpty(query)) continue;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Message message = poebot.ProcessRequest(query);
                sw.Stop();
                Console.WriteLine(message.Text);
                if (message.DoesHaveAnImage())
                    using (MemoryStream stream = new MemoryStream(message.Image()))
                    {
                        Bitmap bmp = new Bitmap(stream); 
                        bmp.Save("image.png", ImageFormat.Png);
                        bmp.Dispose();
                    }
                Console.WriteLine($"Время обработки запроса: {sw.ElapsedMilliseconds} мс");
            }
        }

        private static void UpdateRss(object sender, ElapsedEventArgs e)
        {
            try
            {
                using (var r = XmlReader.Create("https://www.pathofexile.com/news/rss"))
                {
                    var feed = SyndicationFeed.Load(r);
                    var last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                    Console.WriteLine($"{last.Title.Text}\n{last.Links[0].Uri}");
                }
                using (var r = XmlReader.Create("https://ru.pathofexile.com/news/rss"))
                {
                    var feed = SyndicationFeed.Load(r);
                    var last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                    Console.WriteLine($"{last.Title.Text}\n{last.Links[0].Uri}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: {ex.Message} at {GetType()}");
            }
        }

        public new static Type GetType()
        {
            return typeof(Program);
        }
    }
}
