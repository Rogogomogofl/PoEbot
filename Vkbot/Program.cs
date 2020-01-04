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
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;

namespace Vkbot
{
    class Program
    {
        const string cachePath = @"bot/vkcache.txt";
        const string subPath = @"bot/vksub.txt";
        const string logPath = @"bot/vklog.txt";
        static readonly VkApi vkapi = new VkApi();
        static readonly Poebot.Poewatch poewatch = new Poebot.Poewatch();
        static SyndicationItem lastEn, lastRu;
        static Timer rssUpdate;

        private static void Main()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (!File.Exists(subPath)) File.Create(subPath).Close();
            if (!File.Exists(cachePath)) File.Create(cachePath).Close();

            rssUpdate = new Timer(5 * 60 * 1000);
            rssUpdate.Elapsed += UpdateRss;
            rssUpdate.AutoReset = true;
            rssUpdate.Enabled = true;

            Auth();
            LongPollServerResponse s = vkapi.Groups.GetLongPollServer(groupId: 178558335);
            string ts = s.Ts;

            SendMessage(new MessagesSendParams
            {
                Message = "Ready",
                UserId = 37321011
            });

            while (true)
            {
                if (!vkapi.IsAuthorized)
                {
                    Auth();
                    s = vkapi.Groups.GetLongPollServer(groupId: 178558335);
                    ts = s.Ts;
                }
                try
                {
                    BotsLongPollHistoryResponse poll = vkapi.Groups.GetBotsLongPollHistory(
                            new BotsLongPollHistoryParams()
                            { Server = s.Server, Ts = ts, Key = s.Key, Wait = 1 });
                    ts = poll.Ts;
                    if (poll.Updates == null) continue;
                    foreach (var ms in poll.Updates.Where(x => x.Type == GroupUpdateType.MessageNew))
                    {
                        Task.Factory.StartNew(() => ProcessReqest(ms));
                    }
                }
                catch
                {
                    s = vkapi.Groups.GetLongPollServer(groupId: 178558335);
                    ts = s.Ts;
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
            Poebot.Poebot poebot = new Poebot.Poebot(poewatch);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string request;
            if (!string.IsNullOrEmpty(ms.Message.Text)) request = ms.Message.Text;
            else if (ms.Message.Attachments.Count != 0 && ms.Message.Attachments[0].Type.Name == "Link") request = ms.Message.Attachments[0].Instance.ToString();
            else return;

            if (request.Contains("/sub ")) request += $"+{ms.Message.PeerId.ToString()}+{subPath}";
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

            Poebot.Message message = poebot.ProcessRequest(request);
            if (message == null) return;
            List<MediaAttachment> attachments = new List<MediaAttachment>();
            if (message.DoesHaveAnImage())
            {
                try
                {
                    var uploadServer = vkapi.Photo.GetMessagesUploadServer((long)ms.Message.PeerId);
                    var photo = vkapi.Photo.SaveMessagesPhoto(UploadStream(uploadServer.UploadUrl, message.Image()));
                    if (ms.Message.Text.Contains("/i "))
                    {
                        using (StreamWriter stream = new StreamWriter(cachePath, true, Encoding.Default))
                        {
                            stream.WriteLine("{0} {1} {2}", message.SysInfo, photo[0].Id, photo[0].OwnerId);
                        }
                    }
                    attachments.AddRange(photo);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e}");
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
            vkapi.Messages.Send(messagesParams);
        }

        private static void Auth()
        {
            vkapi.Authorize(new ApiAuthParams
            {
                AccessToken = File.ReadAllText("bot/vktoken.txt")
            });
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
                            SendMessage(new MessagesSendParams
                            {
                                Message = lastEn.Title.Text + '\n' + lastEn.Links[0].Uri,
                                PeerId = long.Parse(sub.Split(' ')[0])
                            });
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
                            SendMessage(new MessagesSendParams
                            {
                                Message = lastRu.Title.Text + '\n' + lastRu.Links[0].Uri,
                                PeerId = long.Parse(sub.Split(' ')[0])
                            });
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
