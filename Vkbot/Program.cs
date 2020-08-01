using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BotHandlers;
using BotHandlers.Abstracts;
using BotHandlers.APIs;
using BotHandlers.Methods;
using BotHandlers.Models;
using BotHandlers.Static;
using BotHandlers.Workers;
using Flurl.Http;
using VkBot.Models;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;

namespace VkBot
{
    internal class Program
    {
        private const string CachePath = @"bot/vkcache.txt";
        private const string SubPath = @"bot/vksub.txt";
        private const string LangPath = @"bot/vklang.txt";

        private static readonly VkApi Vkapi = new VkApi();
        private static readonly AbstractApi Api = new PoeApi();
        private static readonly Random Rand = new Random();

        private static RssSubscriber _rssSubscriber;
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

            LongPollServerResponse serverResponse = null;
            string ts = null;

            Console.WriteLine("Working");
            Logger.Log.Info("Working");

            while (true)
            {
                if (!Vkapi.IsAuthorized || serverResponse == null)
                {
                    serverResponse = Auth();
                    ts = serverResponse.Ts;
                }
                else
                {
                    try
                    {
                        var poll = Vkapi.Groups.GetBotsLongPollHistory(
                            new BotsLongPollHistoryParams
                                {Server = serverResponse.Server, Ts = ts, Key = serverResponse.Key, Wait = 1});
                        ts = poll.Ts;
                        if (poll.Updates == null) continue;
                        foreach (var ms in poll.Updates.Where(x => x.Type == GroupUpdateType.MessageNew))
                        {
                            ProcessReqestAsync(ms).ConfigureAwait(false);
                        }
                    }
                    catch (FlurlHttpException)
                    {
                        Thread.Sleep(1000);
                    }
                    catch (LongPollKeyExpiredException)
                    {
                        serverResponse = Vkapi.Groups.GetLongPollServer(178558335);
                        ts = serverResponse.Ts;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error($"{GetType()} {ex}");
                    }
                }
            }
        }

        private static Task ProcessReqestAsync(GroupUpdate gUpdate)
        {
            return Task.Run(() =>
            {
                var poebot = new Poebot(Api,
                    new VkPhoto(CachePath, gUpdate.Message.PeerId ?? throw new Exception("Id is null"), Vkapi.Photo),
                    new ChatLanguage(LangPath, gUpdate.Message.PeerId ?? throw new Exception("Id is null"),
                        _langsDictionary));
                var sw = new Stopwatch();
                sw.Start();
                string request;
                if (!string.IsNullOrEmpty(gUpdate.Message.Text))
                {
                    request = gUpdate.Message.Text;
                }
                else if (gUpdate.Message.Attachments.Any() && gUpdate.Message.Attachments[0].Type.Name == "Link")
                {
                    request = gUpdate.Message.Attachments[0].Instance.ToString();
                }
                else
                {
                    return;
                }

                if (request.Contains("/sub "))
                {
                    request += $"+{gUpdate.Message.PeerId}+{SubPath}";
                }

                var message = poebot.ProcessRequest(request);
                if (message == null)
                {
                    return;
                }

                var attachments = new List<MediaAttachment>();
                var cotent = message.Photo?.GetContent();
                if (cotent != null)
                {
                    attachments.Add(new Photo
                    {
                        Id = long.Parse(cotent[0]),
                        OwnerId = long.Parse(cotent[1])
                    });
                }
                else
                {
                    if (message.Text == null) return;
                }

                sw.Stop();
                SendMessage(new MessagesSendParams
                {
                    Message = message.Text,
                    Attachments = attachments,
                    PeerId = gUpdate.Message.PeerId
                });

                if (!request.Contains("/help"))
                {
                    Logger.Log.Info(
                        $"Запрос: {request}\n\nОтвет:\n{message.Text ?? ""}\nВремя ответа: {sw.ElapsedMilliseconds}");
                }
            });
        }

        private static void SendMessage(MessagesSendParams messagesParams)
        {
            if (!Vkapi.IsAuthorized) return;

            if (messagesParams.Message != null && messagesParams.Message.Length > 4096)
            {
                messagesParams.Message = messagesParams.Message.Substring(0, 4096);
            }

            messagesParams.RandomId = Rand.Next();
            Vkapi.Messages.Send(messagesParams);
        }

        private static LongPollServerResponse Auth()
        {
            while (true)
            {
                try
                {
                    if (!Vkapi.IsAuthorized)
                    {
                        Console.WriteLine("Authorizing...");
                        Vkapi.Authorize(new ApiAuthParams
                        {
                            AccessToken = File.ReadAllText("bot/vktoken.txt")
                        });
                    }
                    return Vkapi.Groups.GetLongPollServer(178558335);
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"{GetType()} {ex}");
                    Thread.Sleep(10000);
                }
            }
        }

        private static void RssUpdated(object sender, RssUpdatedEventArgs e)
        {
            SendMessage(new MessagesSendParams
            {
                PeerId = e.Id,
                Message = e.Message
            });
        }

        public new static Type GetType()
        {
            return typeof(Program);
        }
    }
}