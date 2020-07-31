using System.Collections.Generic;
using System.Linq;
using System.Timers;
using BotHandlers.Models;

namespace BotHandlers.Abstracts
{
    public abstract class AbstractApi
    {
        protected readonly object itemsDataLocker = new object();
        protected readonly Timer updateTimer = new Timer(3600 * 1000);
        
        public string DefaultLeague { get; set; }
        public abstract IEnumerable<string> TradeCategories { get; }
        public abstract bool IsDataLoaded { get; }
        
        public abstract (string Name, string Caregory) ItemSearch(string search);
        public abstract string[] ItemsSearch(string search);
        public abstract (string Name, string League)[] GetCharactersList(string account);
        public abstract string GetAccountName(string character);
        public abstract string[] GetLeagues();
        public abstract PriceData GetPrice(string item, string league, string links);
        public abstract string GetItemCategory(string item);
    }

    public abstract class AbstractApi<T> : AbstractApi
    {
        protected IEnumerable<T> _itemsData;
        public override bool IsDataLoaded
        {
            get
            {
                lock (itemsDataLocker) return _itemsData?.Any() ?? false;
            }
        }
    }
}
