using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using BotHandlers.Abstracts;
using BotHandlers.Models;
using BotHandlers.Static;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotHandlers.APIs
{
    public class PoeApi : AbstractApi<KeyValuePair<string, string[]>>
    {
        private static readonly Timer _requestTimer = new Timer { AutoReset = false, Interval = 500 };
        private static volatile bool _isRequestEnabled = true;

        public float ExaltedPrice { get; private set; }

        public override IEnumerable<string> TradeCategories
        {
            get
            {
                lock (itemsDataLocker)
                {
                    return _itemsData.Select(i => i.Key).ToArray();
                }
            }
        }

        static PoeApi()
        {
            _requestTimer.Elapsed += (sender, args) => _isRequestEnabled = true;
        }

        public PoeApi()
        {
            _itemsData = new Dictionary<string, string[]>();
            DefaultLeague = "Standard";
            LoadItemsdataAsync().Wait();

            updateTimer.Elapsed += OnTimedEventAsync;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;
        }

        private async void OnTimedEventAsync(object sender, ElapsedEventArgs e)
        {
            await LoadItemsdataAsync().ConfigureAwait(false);
        }

        private Task LoadItemsdataAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    var leagues = Leagues();
                    DefaultLeague = leagues.FirstOrDefault(o =>
                        o["startAt"].Value<DateTime>() < DateTime.Now &&
                        o["endAt"].Type != JTokenType.Null &&
                        o["endAt"].Value<DateTime>() > DateTime.Now
                    )["id"].Value<string>();
                }
                catch (Exception ex)
                {
                    Common.Logger?.LogError(ex);
                }

                try
                {
                    var itemsJObject = JObject.Parse(PoeAPIRequest("https://www.pathofexile.com/api/trade/data/items"));
                    var categories = itemsJObject["result"].Value<JArray>();
                    lock (itemsDataLocker)
                    {
                        var _itemsDataDictionary = _itemsData as Dictionary<string, string[]>;
                        _itemsDataDictionary.Clear();
                        foreach (var category in categories)
                        {
                            var label = category["label"].Value<string>();
                            var entries = category["entries"].Value<JArray>();

                            var items = new List<string>();
                            items.AddRange(entries.Select(entry =>
                                entry["name"] == null ? entry["type"].Value<string>() : entry["name"].Value<string>()));

                            if (_itemsDataDictionary.ContainsKey(label))
                            {
                                items.AddRange(_itemsDataDictionary[label]);
                            }

                            _itemsDataDictionary[label] = items.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.Logger?.LogError(ex);
                    updateTimer.Interval = 60 * 1000;
                    return;
                }

                var requestJson = new
                {
                    query = new
                    {
                        status = new
                        {
                            option = "online"
                        },
                        filters = new
                        {
                            trade_filters = new
                            {
                                disabled = false,
                                filters = new
                                {
                                    sale_type = new
                                    {
                                        option = "priced"
                                    },
                                    price = new
                                    {
                                        option = "chaos",
                                        min = 1,
                                        max = 9999
                                    }
                                }
                            }
                        },
                        type = "Exalted Orb"
                    },
                    sort = new
                    {
                        price = "asc"
                    }
                };
                var result = TryFetchTrade(requestJson, DefaultLeague, out var fetchedTrade, out var id);
                if (!result)
                {
                    updateTimer.Interval = 60 * 1000;
                    return;
                }

                var prices = new List<float>();
                foreach (var token in fetchedTrade)
                {
                    var listing = token["listing"].Value<JObject>();
                    var price = listing["price"].Value<JObject>();
                    switch (price["currency"].Value<string>())
                    {
                        case "chaos":
                            {
                                prices.Add(price["amount"].Value<float>());
                                break;
                            }
                    }
                }
                if (prices.Count > 0)
                {
                    ExaltedPrice = prices.Sum() / prices.Count;
                }

                Common.Logger?.LogInfo($"Data loaded for {GetType()}");
                updateTimer.Interval = 3600 * 1000;
            });
        }

        #region публичные методы

        public override (string Name, string Caregory) ItemSearch(string search)
        {
            var regex = new Regex($@"^{search.Replace(" ", @"\D*")}\D*", RegexOptions.IgnoreCase);
            lock (itemsDataLocker)
            {
                foreach (var itemsDataByCategory in _itemsData)
                {
                    var item = itemsDataByCategory.Value.FirstOrDefault(o => regex.IsMatch(o));
                    if (item != null)
                    {
                        return (item, itemsDataByCategory.Key);
                    }
                }
            }

            regex = new Regex($@"{search.Replace(" ", @"\D*")}\D*", RegexOptions.IgnoreCase);
            lock (itemsDataLocker)
            {
                foreach (var itemsDataByCategory in _itemsData)
                {
                    var item = itemsDataByCategory.Value.FirstOrDefault(o => regex.IsMatch(o));
                    if (item != null)
                    {
                        return (item, itemsDataByCategory.Key);
                    }
                }
            }

            return (null, null);
        }

        public override string[] ItemsSearch(string search)
        {
            var pattern = search.Replace("the ", @"the\s").Replace(" ", @"\D*\s\D*");
            var regex = new Regex($@"{pattern}\D*", RegexOptions.IgnoreCase);
            lock (itemsDataLocker)
            {
                return _itemsData.SelectMany(x => x.Value)
                    .Where(i => regex.IsMatch(i)).ToArray();
            }
        }

        public override (string Name, string League)[] GetCharactersList(string account)
        {
            try
            {
                var characters = Characters(account);

                return characters.Select(character =>
                    (character["name"].Value<string>(), character["league"].Value<string>())).ToArray();
            }
            catch
            {
                return null;
            }
        }

        public override string GetAccountName(string character)
        {
            try
            {
                var account = Account(character);

                return account["accountName"].Value<string>();
            }
            catch
            {
                return null;
            }
        }

        public override string[] GetLeagues()
        {
            try
            {
                var leagues = Leagues();

                return leagues.Select(l => l["id"].Value<string>()).ToArray();
            }
            catch
            {
                return null;
            }
        }

        public override PriceData GetPrice(string item, string league, string links)
        {
            object requestJson;
            switch (GetItemCategory(item))
            {
                //Вещи с аттрибутом name 
                case "Accessories":
                case "Armour":
                case "Flasks":
                case "Jewels":
                case "Maps":
                case "Weapons":
                case "Prophecies":
                    {
                        requestJson = new
                        {
                            query = new
                            {
                                status = new
                                {
                                    option = "online"
                                },
                                filters = new
                                {
                                    misc_filters = new
                                    {
                                        disabled = false,
                                        filters = new
                                        {
                                            corrupted = new
                                            {
                                                option = false
                                            }
                                        }
                                    },
                                    socket_filters = new
                                    {
                                        disabled = false,
                                        filters = new
                                        {
                                            links = new
                                            {
                                                min = string.IsNullOrWhiteSpace(links) ? 0 : int.Parse(links)
                                            }
                                        }
                                    },
                                    trade_filters = new
                                    {
                                        disabled = false,
                                        filters = new
                                        {
                                            sale_type = new
                                            {
                                                option = "priced"
                                            }
                                        }
                                    }
                                },
                                name = item
                            },
                            sort = new
                            {
                                price = "asc"
                            }
                        };
                        break;
                    }
                //Вещи без аттрибута name, но с аттрибутом type
                case "Currency":
                case "Cards":
                case "Gems":
                case "Leaguestones":
                case "Itemised Monsters":
                    {
                        requestJson = new
                        {
                            query = new
                            {
                                status = new
                                {
                                    option = "online"
                                },
                                filters = new
                                {
                                    trade_filters = new
                                    {
                                        disabled = false,
                                        filters = new
                                        {
                                            sale_type = new
                                            {
                                                option = "priced"
                                            }
                                        }
                                    }
                                },
                                type = item
                            },
                            sort = new
                            {
                                price = "asc"
                            }
                        };
                        break;
                    }
                default:
                    {
                        return null;
                    }
            }

            var result = TryFetchTrade(requestJson, league, out var trades, out var id);
            if (!result) return null;

            var prices = new List<float>();
            foreach (var token in trades)
            {
                var listing = token["listing"].Value<JObject>();
                var price = listing["price"].Value<JObject>();
                switch (price["currency"].Value<string>())
                {
                    case "chaos":
                    {
                        prices.Add(price["amount"].Value<float>());
                        break;
                    }
                    case "exalted":
                    {
                        prices.Add(price["amount"].Value<float>() * ExaltedPrice);
                        break;
                    }
                }
            }

            if (prices.Count == 0) return null;

            var min = prices[0];
            var median = prices[prices.Count / 2];
            var mean = prices.Sum() / prices.Count;
            var tradeLink = $"https://www.pathofexile.com/trade/search/{league}/{id}";

            var priceData = new PriceData(min, median, mean, mean / ExaltedPrice, tradeLink);
            return priceData;
        }

        public override string GetItemCategory(string item)
        {
            lock (itemsDataLocker)
            {
                foreach (var itemsDataByCategory in _itemsData)
                {
                    if (itemsDataByCategory.Value.Contains(item))
                    {
                        return itemsDataByCategory.Key;
                    }
                }
            }

            return null;
        }

        #endregion

        #region обращения к api

        private static string PoeAPIRequest(string url)
        {
            while (!_isRequestEnabled) {}
            _isRequestEnabled = false;

            var ret = Common.GetContent(url);
            _requestTimer.Start();
            return ret;
        }

        private JArray Leagues()
        {
            return JArray.Parse(PoeAPIRequest("https://www.pathofexile.com/api/leagues"));
        }

        private JArray Characters(string account)
        {
            return JArray.Parse(
                PoeAPIRequest(
                    $"https://www.pathofexile.com/character-window/get-characters?accountName={account}"));
        }

        private JObject Account(string character)
        {
            return JObject.Parse(PoeAPIRequest(
                $"https://www.pathofexile.com/character-window/get-account-name-by-character?character={character}"));
        }

        private bool TryFetchTrade(object requestJson, string league, out JArray trades, out string id, int resultsNumber = 10)
        {
            try
            {
                while (!_isRequestEnabled) { }
                _isRequestEnabled = false;

                var request =
                    (HttpWebRequest)WebRequest.Create($"https://www.pathofexile.com/api/trade/search/{league}");
                var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestJson));
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = data.Length;
                request.UserAgent = "poe-bot";

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                string responseString;
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    responseString = streamReader.ReadToEnd();
                }
                _requestTimer.Start();

                var jo = JObject.Parse(responseString);
                id = jo["id"].Value<string>();
                var result = jo["result"].Value<JArray>();
                var results = string.Join(",", result.Take(resultsNumber).Select(t => t.Value<string>().Trim('"')));

                jo = JObject.Parse(PoeAPIRequest($"https://www.pathofexile.com/api/trade/fetch/{results}?query={id}"));
                trades = jo["result"].Value<JArray>();

                return true;
            }
            catch (Exception ex)
            {
                Common.Logger?.LogError(ex);
                if (!_isRequestEnabled && !_requestTimer.Enabled)
                {
                    _requestTimer.Start();
                }

                id = null;
                trades = null;
                return false;
            }
        }

        #endregion
    }
}