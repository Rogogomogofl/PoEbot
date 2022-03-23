using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotHandlers.Abstracts;
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

namespace VkBot.Workers
{
    class VkBotWorker : AbstractWorker
    {
        private readonly VkApi Vkapi = new VkApi();
        private readonly Random Rand = new Random();
        private readonly ulong _groupid;
        private readonly string _token;

        public VkBotWorker(string cachePath, string subPath, string langPath, double rssUpdateInterval = 5 * 60 * 1000) 
            : base(cachePath, subPath, langPath, rssUpdateInterval)
        {
            _groupid = ulong.Parse(File.ReadAllText("bot/vkgroup.txt"));
            _token = File.ReadAllText("bot/vktoken.txt");
        }

        public override void Work()
        {
            LongPollServerResponse serverResponse = null;
            string ts = null;

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
                            { Server = serverResponse.Server, Ts = ts, Key = serverResponse.Key, Wait = 1 });
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
                        serverResponse =
                            Vkapi.Groups.GetLongPollServer(_groupid);
                        ts = serverResponse.Ts;
                    }
                    catch (Exception ex)
                    {
                        Common.Logger?.LogError(ex);
                    }
                }
            }
        }

        private Task ProcessReqestAsync(GroupUpdate gUpdate)
        {
            return Task.Run(() =>
            {
                var poebot = new Poebot(Api,
                    new VkPhoto(_cachePath, gUpdate.Message.PeerId ?? throw new Exception("Id is null"), Vkapi.Photo),
                    new ChatLanguage(_langPath, gUpdate.Message.PeerId ?? throw new Exception("Id is null"),
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
                    sw.Stop();
                    return;
                }

                if (request.Contains("/sub "))
                {
                    request = $"{request}+{gUpdate.Message.PeerId}+{_subPath}";
                }

                var message = poebot.ProcessRequest(request);
                if (message == null)
                {
                    sw.Stop();
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
                else if (string.IsNullOrEmpty(message.Text))
                {
                    sw.Stop();
                    return;
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
                    Common.Logger?.LogInfo($"Запрос: {request}" +
                                    "\n\nОтвет:" +
                                    $"\n{message.Text ?? ""}" +
                                    $"\nВремя ответа: {sw.ElapsedMilliseconds}" +
                                    $"\n---");
                }
            });
        }
        private LongPollServerResponse Auth()
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
                            AccessToken = _token
                        });
                    }
                    return Vkapi.Groups.GetLongPollServer(_groupid);
                }
                catch (Exception ex)
                {
                    Common.Logger?.LogError(ex);
                    Thread.Sleep(10000);
                }
            }
        }

        private void SendMessage(MessagesSendParams messagesParams)
        {
            if (!Vkapi.IsAuthorized) return;

            if (messagesParams.Message?.Length > 4096)
            {
                messagesParams.Message = messagesParams.Message.Substring(0, 4096);
            }

            messagesParams.RandomId = Rand.Next();
            Vkapi.Messages.Send(messagesParams);
        }

        protected override void RssUpdated(object sender, RssUpdatedEventArgs e)
        {
            SendMessage(new MessagesSendParams
            {
                PeerId = e.Id,
                Message = e.Message
            });
        }
    }
}
