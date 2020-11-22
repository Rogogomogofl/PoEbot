using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BotHandlers.Abstracts;
using BotHandlers.Methods;
using BotHandlers.Models;
using BotHandlers.Static;
using BotHandlers.Workers;
using Telegram.Bot;
using Telegram.Bot.Args;
using TelegramBot.Models;

namespace TelegramBot.Workers
{
    class TelegramBotWorker : AbstractWorker
    {
        private static TelegramBotClient _telegramBot;

        public TelegramBotWorker(string cachePath, string subPath, string langPath, double rssUpdateInterval = 5 * 60 * 1000) 
            : base(cachePath, subPath, langPath, rssUpdateInterval)
        {
        }

        public override void Work()
        {
            _telegramBot = new TelegramBotClient(File.ReadAllText("bot/telegramtoken.txt"));
            _telegramBot.OnMessage += TelegramBot_OnMessage;
            _telegramBot.StartReceiving();

            Console.WriteLine("Working");
            Common.Logger.LogInfo("Working");
            while (_telegramBot.IsReceiving) ;
        }

        private async void TelegramBot_OnMessage(object sender, MessageEventArgs e)
        {
            await Task.Run(() =>
            {
                if (e.Message.Text == null) return;
                if (e.Message.Date.AddMinutes(2) < DateTime.Now.ToUniversalTime()) return;

                var sobaka = e.Message.Text.Split('@');
                if (sobaka.Length > 1)
                {
                    if (sobaka[1] == _telegramBot.GetMeAsync().Result.Username) e.Message.Text = sobaka[0];
                    else return;
                }

                var poebot = new Poebot(Api,
                    new TelegramPhoto(_cachePath, e.Message.Chat.Id, _telegramBot),
                    new ChatLanguage(_langPath, e.Message.Chat.Id, _langsDictionary));
                var sw = new Stopwatch();
                sw.Start();
                var request = e.Message.Text;
                if (request.Contains("/sub ")) request = $"{request}+{e.Message.Chat.Id}+{_subPath}";

                var message = poebot.ProcessRequest(request);
                if (message == null) return;
                if (message.Text != null)
                {
                    _telegramBot.SendTextMessageAsync(e.Message.Chat.Id, message.Text);
                }

                var content = message.Photo?.GetContent();
                if (content != null)
                {
                    _telegramBot.SendPhotoAsync(e.Message.Chat.Id, content[0]);
                }

                sw.Stop();
                if (!(request.Contains("/help") || request.Contains("/start")))
                {
                    Common.Logger.LogInfo($"Запрос: {request}" +
                                    "\n\nОтвет:" +
                                    $"\n{message.Text ?? ""}" +
                                    $"\nВремя ответа: {sw.ElapsedMilliseconds}" +
                                    $"\n---");
                }
            });
        }

        protected override void RssUpdated(object sender, RssUpdatedEventArgs e)
        {
            _telegramBot.SendTextMessageAsync(e.Id, e.Message);
        }
    }
}
