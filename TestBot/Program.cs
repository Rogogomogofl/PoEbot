using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using BotHandlers.APIs;
using BotHandlers.Methods;
using BotHandlers.Mocks;
using BotHandlers.Static;

namespace TestBot
{
    internal class Program
    {
        private static Timer _rssUpdate;

        private static void Main()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Logger.InitLogger();

            _rssUpdate = new Timer(10 * 1000);
            _rssUpdate.Elapsed += UpdateRss;
            _rssUpdate.AutoReset = true;
            //_rssUpdate.Enabled = true;

            var language = new Dictionary<long, ResponseLanguage>
            {
                { 0, ResponseLanguage.Russain }
            };

            var api = new PoeApi();

            Console.WriteLine("Working");

            while (true)
            {
                var poebot = new Poebot(api, new MockPhoto(true), new MockLanguage(language));
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
                Console.WriteLine($"\nВремя обработки запроса: {sw.ElapsedMilliseconds} мс\n");
            }
        }

        private static void UpdateRss(object sender, ElapsedEventArgs e)
        {
            Task.Run(() =>
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
                    Console.WriteLine($"{DateTime.Now}: {GetType()} {ex}");
                }
            });
        }

        public new static Type GetType()
        {
            return typeof(Program);
        }
    }
}
