using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Poebot
{
    public class Poewatch
    {
        public readonly string[] TradeCategories = { "gem", "weapon", "accessory", "armour", "jewel", "flask", "card", "currency", "map", "prophecy" };
        private JArray itemsData = new JArray();
        private readonly object itemsDataLocker = new object();
        private Timer updateTimer = new Timer(3600 * 1000);
        public string DefaultLeague { get; private set; } = "Standard";

        public Poewatch()
        {
            updateTimer.Elapsed += onTimedEvent;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;
            loadItemdata();
        }

        public bool IsDataLoaded()
        {
            lock (itemsDataLocker)
                if (itemsData.Count > 0) return true;
                else return false;
        }

        public JObject FirstOrDefault(Func<JToken, bool> predicate)
        {
            lock (itemsDataLocker) return (JObject)itemsData.FirstOrDefault(predicate);
        }

        public IEnumerable<JToken> Where(Func<JToken, bool> predicate)
        {
            lock (itemsDataLocker) return itemsData.Where(predicate);
        }

        public JArray Leagues()
        {
            return JArray.Parse(Common.GetContent("https://api.poe.watch/leagues"));
        }

        public JObject Item(string id)
        {
            return JObject.Parse(Common.GetContent("https://api.poe.watch/item?id=" + id));
        }

        public JArray ItemData()
        {
            return JArray.Parse(Common.GetContent("https://api.poe.watch/itemdata"));
        }

        public JArray ItemHistory(string id, string league)
        {
            return JArray.Parse(Common.GetContent("https://api.poe.watch/itemhistory?id=" + id + "&league=" + league));
        }

        public JArray Get(string category)
        {
            return JArray.Parse(Common.GetContent("https://api.poe.watch/get?league=" + DefaultLeague + "&category=" + category));
        }

        public JArray Get(string league, string category)
        {
            return JArray.Parse(Common.GetContent("https://api.poe.watch/get?league=" + league + "&category=" + category));
        }

        public JArray Accounts(string character)
        {
            return JArray.Parse(Common.GetContent("https://api.poe.watch/accounts?character=" + character));
        }

        public JArray Characters(string account)
        {
            return JArray.Parse(Common.GetContent("https://api.poe.watch/characters?account=" + account));
        }

        private void onTimedEvent(object sender, ElapsedEventArgs e)
        {
            loadItemdata();
        }

        private void loadItemdata()
        {
            try
            {
                var ja = Leagues();
                DefaultLeague = ja.LastOrDefault(o => !o["hardcore"].Value<bool>() && !o["event"].Value<bool>() && o["challenge"].Value<bool>() && !o["upcoming"].Value<bool>())["name"].Value<string>();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}: {e}");
            }
            try
            {
                lock (itemsDataLocker) itemsData = ItemData();
                updateTimer.Interval = 3600 * 1000;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}: {e}");
                updateTimer.Interval = 5000;
                return;
            }
        }
    }
}
