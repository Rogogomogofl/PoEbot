using Bot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;
using Timer = System.Timers.Timer;

namespace Vkbot
{
    internal class Program
    {
        private const string CachePath = @"bot/vkcache.txt";
        private const string SubPath = @"bot/vksub.txt";
        private const string LogPath = @"bot/vklog.txt";
        private static readonly VkApi Vkapi = new VkApi();
        private static readonly Poewatch Poewatch = new Poewatch();
        private static SyndicationItem _lastEn, _lastRu;
        private static Timer _rssUpdate;

        private static void Main()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (!File.Exists(SubPath)) File.Create(SubPath).Close();
            if (!File.Exists(CachePath)) File.Create(CachePath).Close();

            _rssUpdate = new Timer(5 * 60 * 1000);
            _rssUpdate.Elapsed += UpdateRss;
            _rssUpdate.AutoReset = true;
            _rssUpdate.Enabled = true;

            LongPollServerResponse serverResponse = null;
            string ts = null;

            Console.WriteLine("Working");

            while (true)
            {
                if (!Vkapi.IsAuthorized)
                {
                    serverResponse = Auth();
                    ts = serverResponse.Ts;
                }
                try
                {
                    var poll = Vkapi.Groups.GetBotsLongPollHistory(
                            new BotsLongPollHistoryParams()
                            { Server = serverResponse.Server, Ts = ts, Key = serverResponse.Key, Wait = 1 });
                    ts = poll.Ts;
                    if (poll.Updates == null) continue;
                    foreach (var ms in poll.Updates.Where(x => x.Type == GroupUpdateType.MessageNew))
                    {
                        Task.Factory.StartNew(() => ProcessReqest(ms));
                    }
                }
                catch
                {
                    Thread.Sleep(1000);
                    serverResponse = Vkapi.Groups.GetLongPollServer(groupId: 178558335);
                    ts = serverResponse.Ts;
                }

            }
        }

        private static string UploadStream(string url, byte[] data)
        {
            using (var requestContent = new MultipartFormDataContent())
            using (var imageContent = new ByteArrayContent(data))
            {
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                requestContent.Add(imageContent, "photo", "image.png");
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.PostAsync(url, requestContent).Result;
                    return response.Content.ReadAsStringAsync().Result;
                }
            }
        }

        private static void ProcessReqest(GroupUpdate ms)
        {
            Poebot poebot = new Poebot(Poewatch);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string request;
            if (!string.IsNullOrEmpty(ms.Message.Text)) request = ms.Message.Text;
            else if (ms.Message.Attachments.Any() && ms.Message.Attachments[0].Type.Name == "Link") request = ms.Message.Attachments[0].Instance.ToString();
            else return;

            if (request.Contains("/sub ")) request += $"+{ms.Message.PeerId}+{SubPath}";
            if (request.Contains("/i "))
            {
                string item = poebot.GetItemName(Regex.Split(request, @"/i ")[1]);
                if (!string.IsNullOrEmpty(item))
                {
                    item = item.ToLower().Replace(' ', '-').Replace("'", "");
                    string[] lines = File.ReadAllLines(CachePath);
                    foreach (string line in lines)
                    {
                        var data = line.Split(' ');
                        if (data[0] == item)
                        {
                            sw.Stop();
                            SendMessage(new MessagesSendParams
                            {
                                Attachments = new List<MediaAttachment>
                                    {
                                        new Photo
                                        {
                                            Id = long.Parse(data[1]),
                                            OwnerId = long.Parse(data[2])
                                        }
                                    },
                                PeerId = ms.Message.PeerId
                            });
                            Log(request, "", sw.ElapsedMilliseconds.ToString());
                            return;
                        }
                    }
                }
            }

            Bot.Message message = poebot.ProcessRequest(request);
            if (message == null) return;
            List<MediaAttachment> attachments = new List<MediaAttachment>();
            if (message.DoesHaveAnImage())
            {
                try
                {
                    var uploadServer = Vkapi.Photo.GetMessagesUploadServer((long)ms.Message.PeerId);
                    var photo = Vkapi.Photo.SaveMessagesPhoto(UploadStream(uploadServer.UploadUrl, message.Image()));
                    if (ms.Message.Text.Contains("/i "))
                    {
                        using (StreamWriter stream = new StreamWriter(CachePath, true, Encoding.Default))
                        {
                            stream.WriteLine("{0} {1} {2}", message.SysInfo, photo[0].Id, photo[0].OwnerId);
                        }
                    }
                    attachments.AddRange(photo);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                    return;
                }
            }
            if (message.LoadedPhoto != null)
            {
                attachments.Add(new Photo
                {
                    Id = message.LoadedPhoto.VkId,
                    OwnerId = message.LoadedPhoto.VkOwnerId
                });
            }
            sw.Stop();
            SendMessage(new MessagesSendParams
            {
                Message = message.Text,
                Attachments = attachments,
                PeerId = ms.Message.PeerId
            });
            if (!request.Contains("/help"))
                Log(request, message.Text ?? "", sw.ElapsedMilliseconds.ToString());
        }

        private static void SendMessage(MessagesSendParams messagesParams)
        {
            Random random = new Random();
            if (messagesParams.Message != null && messagesParams.Message.Count() > 4096) messagesParams.Message = messagesParams.Message.Substring(0, 4096);
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
                    Console.WriteLine(e.Message);
                    Thread.Sleep(10000);
                }
            }
            throw new Exception("UB");
        }

        private static void UpdateRss(object sender, ElapsedEventArgs e)
        {
            try
            {
                List<string> subs = File.ReadAllLines(SubPath).ToList();
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
                Console.WriteLine($"{DateTime.Now}: {ex.Message} at {GetType()}");
            }
        }

        public new static Type GetType()
        {
            return typeof(Program);
        }

        private static void Log(string request, string responce, string time)
        {
            using (StreamWriter streamWriter = new StreamWriter(LogPath, true, Encoding.Default))
            {
                streamWriter.WriteLine($"{DateTime.Now}\nЗапрос:\n{request}\n\nОтвет:\n{responce}\nВремя ответа: {time}\n------------");
            }
        }
    }
}
