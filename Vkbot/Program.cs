using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using BotHandlers;
using Flurl.Http;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;
using Timer = System.Timers.Timer;

namespace VkBot
{
    internal class Program
    {
        private const string CachePath = @"bot/vkcache.txt";
        private const string SubPath = @"bot/vksub.txt";
        private const string LangPath = @"bot/vklang.txt";
        private static readonly VkApi Vkapi = new VkApi();
        private static readonly Poewatch Poewatch = new Poewatch();
        private static SyndicationItem _lastEn, _lastRu;
        private static Timer _rssUpdate;
        private static readonly Dictionary<long, ResponceLanguage> LangsDictionary = new Dictionary<long, ResponceLanguage>();

        private static void Main()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (!File.Exists(SubPath)) File.Create(SubPath).Close();
            if (!File.Exists(CachePath)) File.Create(CachePath).Close();
            if (!File.Exists(LangPath)) File.Create(LangPath).Close();

            Logger.InitLogger();

            ChatLanguage.LoadDictionary(LangPath, LangsDictionary);

            _rssUpdate = new Timer(5 * 60 * 1000);
            _rssUpdate.Elapsed += UpdateRss;
            _rssUpdate.AutoReset = true;
            _rssUpdate.Enabled = true;

            LongPollServerResponse serverResponse = null;
            string ts = null;

            Console.WriteLine("Working");
            Logger.Log.Info("Working");

            while (true)
            {
                if (!Vkapi.IsAuthorized)
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
                            ProcessReqestAsync(ms);
                        }
                    }
                    catch (FlurlHttpException)
                    {
                        Thread.Sleep(1000);
                    }
                    catch (LongPollKeyExpiredException)
                    {
                        serverResponse = Vkapi.Groups.GetLongPollServer(groupId: 178558335);
                        ts = serverResponse.Ts;
                    }
                    catch (Exception e)
                    {
                        Logger.Log.Error($"{e.Message} at {GetType()}");
                        throw;
                    }
                }
            }
        }

        private static Task ProcessReqestAsync(GroupUpdate ms)
        {
            return Task.Run(() =>
            {
                var poebot = new Poebot(Poewatch,
                    new VkPhoto(CachePath, ms.Message.PeerId ?? throw new Exception("Id is null"), Vkapi.Photo),
                    new ChatLanguage(LangPath, ms.Message.PeerId ?? throw new Exception("Id is null"),
                        LangsDictionary));
                var sw = new Stopwatch();
                sw.Start();
                string request;
                if (!string.IsNullOrEmpty(ms.Message.Text)) request = ms.Message.Text;
                else if (ms.Message.Attachments.Any() && ms.Message.Attachments[0].Type.Name == "Link")
                    request = ms.Message.Attachments[0].Instance.ToString();
                else return;

                if (request.Contains("/sub ")) request += $"+{ms.Message.PeerId}+{SubPath}";

                var message = poebot.ProcessRequest(request);
                if (message == null) return;
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
                    PeerId = ms.Message.PeerId
                });

                if (!request.Contains("/help"))
                    Logger.Log.Info(
                        $"Запрос: {request}\n\nОтвет:\n{message.Text ?? ""}\nВремя ответа: {sw.ElapsedMilliseconds}");
            });
        }

        private static void SendMessage(MessagesSendParams messagesParams)
        {
            var random = new Random();
            if (messagesParams.Message != null && messagesParams.Message.Count() > 4096)
                messagesParams.Message = messagesParams.Message.Substring(0, 4096);
            messagesParams.RandomId = random.Next();
            Vkapi.Messages.Send(messagesParams);
        }

        private static LongPollServerResponse Auth()
        {
            while (!Vkapi.IsAuthorized)
            {
                Console.WriteLine("Authorizing...");
                try
                {
                    Vkapi.Authorize(new ApiAuthParams
                    {
                        AccessToken = File.ReadAllText("bot/vktoken.txt")
                    });
                    return Vkapi.Groups.GetLongPollServer(groupId: 178558335);
                }
                catch (Exception e)
                {
                    Logger.Log.Error($"{e.Message} at {GetType()}");
                    Thread.Sleep(10000);
                }
            }

            throw new Exception("UB");
        }

        private static void UpdateRss(object sender, ElapsedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    var subs = File.ReadAllLines(SubPath).ToList();
                    using (var r = XmlReader.Create("https://www.pathofexile.com/news/rss"))
                    {
                        var feed = SyndicationFeed.Load(r);
                        var last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                        if (_lastEn == null) _lastEn = last;
                        if (last.Links[0].Uri != _lastEn.Links[0].Uri && last.PublishDate > _lastEn.PublishDate)
                        {
                            _lastEn = last;
                            var enSubs = subs.Where(x => Regex.IsMatch(x, @"\d+\sen"));
                            foreach (var sub in enSubs)
                                SendMessage(new MessagesSendParams
                                {
                                    Message = _lastEn.Title.Text + '\n' + _lastEn.Links[0].Uri,
                                    PeerId = long.Parse(sub.Split(' ')[0])
                                });
                        }
                    }

                    using (var r = XmlReader.Create("https://ru.pathofexile.com/news/rss"))
                    {
                        var feed = SyndicationFeed.Load(r);
                        var last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                        if (_lastRu == null) _lastRu = last;
                        if (last.Links[0].Uri != _lastRu.Links[0].Uri && last.PublishDate > _lastRu.PublishDate)
                        {
                            _lastRu = last;
                            var ruSubs = subs.Where(x => Regex.IsMatch(x, @"\d+\sru"));
                            foreach (var sub in ruSubs)
                                SendMessage(new MessagesSendParams
                                {
                                    Message = _lastRu.Title.Text + '\n' + _lastRu.Links[0].Uri,
                                    PeerId = long.Parse(sub.Split(' ')[0])
                                });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"{ex.Message} at {GetType()}");
                }
            });
        }

        public new static Type GetType()
        {
            return typeof(Program);
        }
    }
}