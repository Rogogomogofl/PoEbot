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
        static string cachePath = @"bot/vkcache.txt";
        static string subPath = @"bot/vksub.txt";
        static string logPath = @"bot/vklog.txt";
        static VkApi vkapi = new VkApi();
        static Poebot.Poebot poebot = new Poebot.Poebot();
        static SyndicationItem lastEn = null, lastRu = null;
        static Timer rssUpdate;

        private static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (!File.Exists(subPath)) File.Create(subPath).Close();
            if (!File.Exists(cachePath)) File.Create(cachePath).Close();

            rssUpdate = new Timer(5 * 60 * 1000);
            rssUpdate.Elapsed += updateRss;
            rssUpdate.AutoReset = true;
            rssUpdate.Enabled = true;

            auth();
            LongPollServerResponse s = vkapi.Groups.GetLongPollServer(groupId: 178558335);
            string Ts = s.Ts;

            sendMessage(new MessagesSendParams
            {
                Message = "Ready",
                UserId = 37321011
            });

            while (true)
            {
                if (!vkapi.IsAuthorized)
                {
                    auth();
                    s = vkapi.Groups.GetLongPollServer(groupId: 178558335);
                    Ts = s.Ts;
                }
                try
                {
                    BotsLongPollHistoryResponse poll = vkapi.Groups.GetBotsLongPollHistory(
                            new BotsLongPollHistoryParams()
                            { Server = s.Server, Ts = Ts, Key = s.Key, Wait = 1 });
                    Ts = poll.Ts;
                    if (poll?.Updates == null) continue;
                    foreach (var ms in poll.Updates.Where(x => x.Type == GroupUpdateType.MessageNew))
                    {
                        Task.Factory.StartNew(() => processReqest(ms));
                    }
                }
                catch
                {
                    s = vkapi.Groups.GetLongPollServer(groupId: 178558335);
                    Ts = s.Ts;
                }

            }
        }

        private static string uploadStream(string url, byte[] data)
        {
            using (var requestContent = new MultipartFormDataContent())
            {
                var imageContent = new ByteArrayContent(data);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                requestContent.Add(imageContent, "photo", "image.png");
                using (var _httpClient = new HttpClient())
                {
                    var responce = _httpClient.PostAsync(url, requestContent).Result;
                    return responce.Content.ReadAsStringAsync().Result;
                }
            }
        }

        private static void processReqest(GroupUpdate ms)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string request;
            if (ms.Message.Text != string.Empty) request = ms.Message.Text;
            else if (ms.Message.Attachments.Count != 0 && ms.Message.Attachments[0].Type.Name == "Link") request = ms.Message.Attachments[0].Instance.ToString();
            else return;

            if (request.Contains("/sub ")) request += $"+{ms.Message.PeerId.ToString()}+{subPath}";
            if (request.Contains("/i "))
            {
                string item = poebot.GetItemName(Regex.Split(request, @"/i ")[1]);
                if (item != string.Empty)
                {
                    item = item.ToLower().Replace(' ', '-').Replace("'", "");
                    string[] lines = File.ReadAllLines(cachePath);
                    foreach (string line in lines)
                    {
                        var data = line.Split(' ');
                        if (data[0] == item)
                        {
                            sw.Stop();
                            sendMessage(new MessagesSendParams
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
            if (message.Image != null)
            {
                try
                {
                    var uploadServer = vkapi.Photo.GetMessagesUploadServer((long)ms.Message.PeerId);
                    var photo = vkapi.Photo.SaveMessagesPhoto(uploadStream(uploadServer.UploadUrl, message.Image));
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
                    Console.WriteLine(e.Message);
                    return;
                }
            }
            if (message.Loaded_Photo != null)
            {
                attachments.Add(new Photo
                {
                    Id = message.Loaded_Photo.VkId,
                    OwnerId = message.Loaded_Photo.VkOwnerId
                });
            }
            sw.Stop();
            sendMessage(new MessagesSendParams
            {
                Message = message.Text,
                Attachments = attachments,
                PeerId = ms.Message.PeerId
            });
            if (!request.Contains("/help"))
                Log(request, message.Text ?? "", sw.ElapsedMilliseconds.ToString());
        }

        private static void sendMessage(MessagesSendParams messagesParams)
        {
            Random random = new Random();
            if (messagesParams.Message != null && messagesParams.Message.Count() > 4096) messagesParams.Message = messagesParams.Message.Substring(0, 4096);
            messagesParams.RandomId = random.Next();
            vkapi.Messages.Send(messagesParams);
        }

        private static void auth()
        {
            vkapi.Authorize(new ApiAuthParams
            {
                AccessToken = File.ReadAllText("bot/vktoken.txt")
            });
        }

        private static void updateRss(object sender, ElapsedEventArgs e)
        {
            try
            {
                List<string> subs = File.ReadAllLines(subPath).ToList();
                var r = XmlReader.Create("https://www.pathofexile.com/news/rss");
                var feed = SyndicationFeed.Load(r);
                var last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                if (lastEn == null) lastEn = last;
                if (last.Links[0].Uri != lastEn.Links[0].Uri)
                {
                    lastEn = last;
                    var enSubs = subs.Where(x => Regex.IsMatch(x, @"\d+\sen"));
                    foreach (var sub in enSubs)
                        sendMessage(new MessagesSendParams
                        {
                            Message = lastEn.Title.Text + '\n' + lastEn.Links[0].Uri,
                            PeerId = long.Parse(sub.Split(' ')[0])
                        });
                }
                r = XmlReader.Create("https://ru.pathofexile.com/news/rss");
                feed = SyndicationFeed.Load(r);
                last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                if (lastRu == null) lastRu = last;
                if (last.Links[0].Uri != lastRu.Links[0].Uri)
                {
                    lastRu = last;
                    var ruSubs = subs.Where(x => Regex.IsMatch(x, @"\d+\sru"));
                    foreach (var sub in ruSubs)
                        sendMessage(new MessagesSendParams
                        {
                            Message = lastRu.Title.Text + '\n' + lastRu.Links[0].Uri,
                            PeerId = long.Parse(sub.Split(' ')[0])
                        });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
