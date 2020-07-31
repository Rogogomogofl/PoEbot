using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BotHandlers;
using BotHandlers.Abstracts;
using BotHandlers.APIs;
using MihaZupan;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace TelegramBot
{
    internal class Program
    {
        private const string SubPath = @"bot/telegramsub.txt";
        private const string CachePath = @"bot/telegramcache.txt";
        private const string LangPath = @"bot/telegramlang.txt";

        private static readonly AbstractApi Api = new PoeApi();

        private static RssSubscriber _rssSubscriber;
        private static TelegramBotClient _telegramBot;
        private static Dictionary<long, ResponseLanguage> _langsDictionary;

        private static void Main()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (!File.Exists(SubPath)) File.Create(SubPath).Close();
            if (!File.Exists(CachePath)) File.Create(CachePath).Close();
            if (!File.Exists(LangPath)) File.Create(LangPath).Close();

            Logger.InitLogger();

            _langsDictionary = ChatLanguage.LoadDictionary(LangPath);
            _rssSubscriber = new RssSubscriber(SubPath);
            _rssSubscriber.RssUpdated += RssUpdated;

            var proxy = new HttpToSocks5Proxy("103.111.183.18", 1080)
            {
                ResolveHostnamesLocally = true
            };
            _telegramBot = new TelegramBotClient(File.ReadAllText("bot/telegramtoken.txt"), proxy);
            //var result = telegramBot.GetMeAsync().Result;
            _telegramBot.OnMessage += TelegramBot_OnMessage;
            _telegramBot.StartReceiving();
            Console.WriteLine("Working");
            Logger.Log.Info("Working");
            while (true) ;
        }

        private static async void TelegramBot_OnMessage(object sender, MessageEventArgs e)
        {
            await Task.Run(() =>
            {
                if (e.Message.Text == null) return;
                if (e.Message.Date.AddMinutes(2) < DateTime.Now.ToUniversalTime()) return;
                var sobaka = e.Message.Text.Split('@');
                if (sobaka.Length > 1)
                {
                    if (sobaka[1] == "poeinfobot") e.Message.Text = sobaka[0];
                    else return;
                }

                var poebot = new Poebot(Api, 
                    new TelegramPhoto(CachePath, e.Message.Chat.Id, _telegramBot),
                    new ChatLanguage(LangPath, e.Message.Chat.Id, _langsDictionary));
                var sw = new Stopwatch();
                sw.Start();
                var request = e.Message.Text;
                if (request.Contains("/sub ")) request += "+" + e.Message.Chat.Id + "+" + SubPath;

                var message = poebot.ProcessRequest(request);
                if (message == null) return;
                if (message.Text != null)
                    _telegramBot.SendTextMessageAsync(e.Message.Chat.Id, message.Text);
                var content = message.Photo?.GetContent();
                if (content != null)
                {
                    _telegramBot.SendPhotoAsync(e.Message.Chat.Id, content[0]);
                }

                sw.Stop();
                if (!(request.Contains("/help") || request.Contains("/start")))
                    Logger.Log.Info($"Запрос: {request}\n\nОтвет:\n{message.Text ?? ""}\nВремя ответа: {sw.ElapsedMilliseconds}");
            });
        }

        private static void RssUpdated(object sender, RssUpdatedEventArgs e)
        {
            _telegramBot.SendTextMessageAsync(e.Id, e.Message);
        }
    }
}