using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using BotHandlers.Abstracts;
using BotHandlers.Models;
using BotHandlers.Static;
using Newtonsoft.Json.Linq;

namespace BotHandlers.APIs
{
    class PoeNinjaAPI : AbstractApi<JObject>
    {
        private readonly string[] tradeCategories = new[]
        {
            "Currency",
            "Fragment",
            "Seed"
        };
        public override IEnumerable<string> TradeCategories => tradeCategories;

        public PoeNinjaAPI(string defaultLeague)
        {
            _itemsData = new List<JObject>();
            DefaultLeague = defaultLeague;
            updateTimer.Elapsed += OnTimedEvent;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;
            LoadItemsdataAsync();
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            LoadItemsdataAsync();
        }

        private async void LoadItemsdataAsync()
        {
            await Task.Run(() =>
            {
                try
                {

                    Common.Logger.LogInfo($"Data loaded for {GetType()}");
                    updateTimer.Interval = 3600 * 1000;
                }
                catch (Exception ex)
                {
                    Common.Logger.LogError(ex);
                    updateTimer.Interval = 60 * 1000;
                }
            });
        }

        public override (string Name, string Caregory) ItemSearch(string search)
        {
            throw new NotImplementedException();
        }

        public override string[] ItemsSearch(string search)
        {
            throw new NotImplementedException();
        }

        public override (string Name, string League)[] GetCharactersList(string account)
        {
            throw new NotImplementedException();
        }

        public override string GetAccountName(string character)
        {
            throw new NotImplementedException();
        }

        public override string[] GetLeagues()
        {
            throw new NotImplementedException();
        }

        public override PriceData GetPrice(string item, string league, string links)
        {
            throw new NotImplementedException();
        }

        public override string GetItemCategory(string item)
        {
            throw new NotImplementedException();
        }
    }
}
