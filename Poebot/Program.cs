using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Timers;
using System.Xml;

namespace Bot
{
    internal class Program
    {
        private static Timer _rssUpdate;

        private static void Main()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            _rssUpdate = new Timer(10 * 1000);
            _rssUpdate.Elapsed += UpdateRss;
            _rssUpdate.AutoReset = true;
            //rssUpdate.Enabled = true;

            var poewatch = new Poewatch();
            Console.WriteLine("Working");
            while (true)
            {
                var poebot = new Poebot(poewatch, new TestPhoto());
                var query = Console.ReadLine();
                if (string.IsNullOrEmpty(query)) continue;
                var sw = new Stopwatch();
                sw.Start();
                var message = poebot.ProcessRequest(query);
                sw.Stop();
                if (message == null)
                {
                    Console.WriteLine("Некорректный запрос");
                    continue;
                }

                Console.WriteLine(message.Text);
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