using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using BotHandlers.APIs.PoE;
using BotHandlers.Logger;
using BotHandlers.Methods;
using BotHandlers.Models;
using BotHandlers.Static;
using BotHandlers.Workers;

namespace BotHandlers.Abstracts
{
    public abstract class AbstractWorker
    {
        protected readonly string _cachePath;
        protected readonly string _subPath;
        protected readonly string _langPath;
        protected readonly RssSubscriber _rssSubscriber;
        protected readonly Dictionary<long, ResponseLanguage> _langsDictionary;
        protected readonly AbstractApi Api;

        protected AbstractWorker(string cachePath, string subPath, string langPath, double rssUpdateInterval)
        {
            _subPath = subPath;
            _cachePath = cachePath;
            _langPath = langPath;

            Common.Logger = new FileLogger(GetType().Name);

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Api = new PoeApi();

            if (!File.Exists(_subPath)) File.Create(_subPath).Close();
            if (!File.Exists(_cachePath)) File.Create(_cachePath).Close();
            if (!File.Exists(_langPath)) File.Create(_langPath).Close();

            _langsDictionary = ChatLanguage.LoadDictionary(_langPath);
            _rssSubscriber = new RssSubscriber(_subPath, rssUpdateInterval);
            _rssSubscriber.RssUpdated += RssUpdated;

            var msg = $"{GetType()} loaded";
            Common.Logger?.LogInfo(msg);
            Console.WriteLine(msg);
        }

        public abstract void Work();

        protected abstract void RssUpdated(object sender, RssUpdatedEventArgs e);
    }
}
