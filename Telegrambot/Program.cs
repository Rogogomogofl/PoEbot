using MihaZupan;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Telegram.Bot;

namespace Telegrambot
{
    class Program
    {
        static TelegramBotClient telegramBot;
        const string subPath = @"bot/telegramsub.txt";
        const string cachePath = @"bot/telegramcache.txt";
        const string langPath = @"bot/telegramlang.txt";
        const string logPath = @"bot/telegramlog.txt";
        static readonly Poebot.Poewatch poewatch = new Poebot.Poewatch();
        static SyndicationItem lastEn = null, lastRu = null;
        static Timer rssUpdate;

        static void Main()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (!File.Exists(subPath)) File.Create(subPath).Close();
            if (!File.Exists(cachePath)) File.Create(cachePath).Close();
            if (!File.Exists(langPath)) File.Create(langPath).Close();
            if (!File.Exists(logPath)) File.Create(logPath).Close();

            rssUpdate = new Timer(5 * 60 * 1000);
            rssUpdate.Elapsed += UpdateRss;
            rssUpdate.AutoReset = true;
            rssUpdate.Enabled = true;

            var proxy = new HttpToSocks5Proxy("124.41.211.78", 42807)
            {
                ResolveHostnamesLocally = true
            };
            telegramBot = new TelegramBotClient(File.ReadAllText("bot/telegramtoken.txt"), proxy);
            telegramBot.OnMessage += TelegramBot_OnMessage;
            telegramBot.StartReceiving();
            telegramBot.SendTextMessageAsync(chatId: 792056367, text: "Ready");
            while (true) ;
        }

        private static async void TelegramBot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
#pragma warning disable CA2007 // Попробуйте вызвать ConfigureAwait для ожидаемой задачи
            await Task.Run(() =>
            {
                if (e.Message.Text != null)
                {
                    if (e.Message.Date.AddMinutes(2) < DateTime.Now.ToUniversalTime()) return;
                    var sobaka = e.Message.Text.Split('@');
                    if (sobaka.Length > 1)
                    {
                        if (sobaka[1] == "poeinfobot") e.Message.Text = sobaka[0];
                        else return;
                    }
                    var chats = File.ReadAllLines(langPath);//to do
                    Poebot.Poebot poebot = new Poebot.Poebot(poewatch);
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    string request = e.Message.Text;
                    if (request.Contains("/sub ")) request += "+" + e.Message.Chat.Id + "+" + subPath;
                    if (request.Contains("/i "))
                    {
                        string item = poebot.GetItemName(Regex.Split(request, @"/i ")[1]);
                        if (!string.IsNullOrEmpty(item))
                        {
                            item = item.ToLower().Replace(' ', '-').Replace("'", "");
                            string[] lines = File.ReadAllLines(cachePath);
                            foreach (string line in lines)
                            {
                                var data = line.Split(' ');
                                if (data[0] == item)
                                {
                                    sw.Stop();
                                    telegramBot.SendPhotoAsync(chatId: e.Message.Chat.Id, photo: data[1]);
                                    Log(request, "", sw.ElapsedMilliseconds.ToString());
                                    return;
                                }
                            }
                        }
                    }
                    Poebot.Message message = poebot.ProcessRequest(request);
                    if (message == null) return;
                    if (message.Text != null) telegramBot.SendTextMessageAsync(chatId: e.Message.Chat.Id, text: message.Text);
                    if (message.Image != null)
                    {
                        using (MemoryStream stream = new MemoryStream(message.Image))
                        {
                            var returnedMessage = telegramBot.SendPhotoAsync(chatId: e.Message.Chat.Id, photo: stream).Result;
                            if (request.Contains("/i "))
                            {
                                using (StreamWriter streamWriter = new StreamWriter(cachePath, true, Encoding.Default))
                                {
                                    streamWriter.WriteLine("{0} {1}", message.SysInfo, returnedMessage.Photo.Last().FileId);
                                }
                            }
                        }
                    }
                    if (message.Loaded_Photo != null) telegramBot.SendPhotoAsync(chatId: e.Message.Chat.Id, photo: message.Loaded_Photo.TelegramId);
                    sw.Stop();
                    if (!(request.Contains("/help") || request.Contains("/start")))
                        Log(request, message.Text ?? "", sw.ElapsedMilliseconds.ToString());
                }
            });
#pragma warning restore CA2007 // Попробуйте вызвать ConfigureAwait для ожидаемой задачи
        }

        private static void UpdateRss(object sender, ElapsedEventArgs e)
        {
            try
            {
                List<string> subs = File.ReadAllLines(subPath).ToList();
                using (var r = XmlReader.Create("https://www.pathofexile.com/news/rss"))
                {
                    var feed = SyndicationFeed.Load(r);
                    var last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                    if (lastEn == null) lastEn = last;
                    if (last.Links[0].Uri != lastEn.Links[0].Uri)
                    {
                        lastEn = last;
                        var enSubs = subs.Where(x => Regex.IsMatch(x, @"\d+\sen"));
                        foreach (var sub in enSubs)
                            telegramBot.SendTextMessageAsync(chatId: long.Parse(sub.Split(' ')[0]), text: lastEn.Title.Text + '\n' + lastEn.Links[0].Uri);
                    }
                }
                using (var r = XmlReader.Create("https://ru.pathofexile.com/news/rss"))
                {
                    var feed = SyndicationFeed.Load(r);
                    var last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                    if (lastRu == null) lastRu = last;
                    if (last.Links[0].Uri != lastRu.Links[0].Uri)
                    {
                        lastRu = last;
                        var ruSubs = subs.Where(x => Regex.IsMatch(x, @"\d+\sru"));
                        foreach (var sub in ruSubs)
                            telegramBot.SendTextMessageAsync(chatId: long.Parse(sub.Split(' ')[0]), text: lastRu.Title.Text + '\n' + lastRu.Links[0].Uri);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: {ex}");
            }
        }

        private static void Log(string request, string responce, string time)
        {
            using (StreamWriter streamWriter = new StreamWriter(logPath, true, Encoding.Default))
            {
                streamWriter.WriteLine("Запрос:\n" + request
                                + "\n\nОтвет:\n" + responce
                                + "\nВремя ответа: " + time
                                + "\n------------");
            }
        }
    }
}
