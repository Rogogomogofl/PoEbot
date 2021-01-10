using System;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using BotHandlers.Static;

namespace BotHandlers.Workers
{
    public class RssUpdatedEventArgs : EventArgs
    {
        public long Id { get; }
        public string Message { get; }

        public RssUpdatedEventArgs(long id, string message)
        {
            Id = id;
            Message = message;
        }
    }

    public class RssSubscriber
    {
        private SyndicationItem _lastEn, _lastRu;
        private readonly string _subPath;
        private readonly Timer _rssUpdater;

        public event EventHandler<RssUpdatedEventArgs> RssUpdated;

        public RssSubscriber(string subPath, double updateInterval)
        {
            _subPath = subPath;

            _rssUpdater = new Timer(updateInterval);
            _rssUpdater.Elapsed += UpdateRssAsync;
            _rssUpdater.AutoReset = true;
            _rssUpdater.Start();
        }

        private async void UpdateRssAsync(object sender, ElapsedEventArgs e)
        {
            await Task.Run(() =>
            {
                try
                {
                    var subs = File.ReadAllLines(_subPath).ToList();
                    using (var r = XmlReader.Create("https://www.pathofexile.com/news/rss"))
                    {
                        var feed = SyndicationFeed.Load(r);
                        var last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                        _lastEn ??= last;
                        if (last.Links[0].Uri != _lastEn.Links[0].Uri && last.PublishDate > _lastEn.PublishDate)
                        {
                            _lastEn = last;
                            var enSubs = subs.Where(x => Regex.IsMatch(x, @"\d+\sen"));
                            foreach (var sub in enSubs)
                            {
                                RssUpdated?.Invoke(this,
                                    new RssUpdatedEventArgs(long.Parse(sub.Split(' ')[0]),
                                        _lastEn.Title.Text + '\n' + _lastEn.Links[0].Uri));
                            }
                        }
                    }

                    using (var r = XmlReader.Create("https://ru.pathofexile.com/news/rss"))
                    {
                        var feed = SyndicationFeed.Load(r);
                        var last = feed.Items.OrderByDescending(x => x.PublishDate).First();
                        _lastRu ??= last;
                        if (last.Links[0].Uri != _lastRu.Links[0].Uri && last.PublishDate > _lastRu.PublishDate)
                        {
                            _lastRu = last;
                            var ruSubs = subs.Where(x => Regex.IsMatch(x, @"\d+\sru"));
                            foreach (var sub in ruSubs)
                            {
                                RssUpdated?.Invoke(this,
                                    new RssUpdatedEventArgs(long.Parse(sub.Split(' ')[0]),
                                        _lastRu.Title.Text + '\n' + _lastRu.Links[0].Uri));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.Logger?.LogError(ex);
                }
            });
        }
    }
}
