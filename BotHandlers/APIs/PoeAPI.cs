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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotHandlers.APIs
{
    public class PoeApi : AbstractApi<KeyValuePair<string, string[]>>
    {
        public float ExaltedPrice { get; set; }
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

        public PoeApi()
        {
            _itemsData = new Dictionary<string, string[]>();
            DefaultLeague = "Standard";
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
                    var leagues = Leagues();
                    DefaultLeague = leagues.FirstOrDefault(o =>
                        o["startAt"].Value<DateTime>() < DateTime.Now &&
                        o["endAt"].Type != JTokenType.Null &&
                        o["endAt"].Value<DateTime>() > DateTime.Now
                    )["id"].Value<string>();
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"{GetType()} {ex}");
                }

                try
                {
                    var itemsJObject = JObject.Parse(Common.GetContent("https://www.pathofexile.com/api/trade/data/items"));
                    var categories = itemsJObject["result"].Value<JArray>();
                    lock (itemsDataLocker)
                    {
                        (_itemsData as Dictionary<string, string[]>).Clear();
                        foreach (var category in categories)
                        {
                            var label = category["label"].Value<string>();
                            var entries = category["entries"].Value<JArray>();

                            var items = new List<string>();
                            items.AddRange(entries.Select(entry =>
                                entry["name"] == null ? entry["type"].Value<string>() : entry["name"].Value<string>()));

                            (_itemsData as Dictionary<string, string[]>).Add(label, items.ToArray());
                        }
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
                    var fetchedTrade = FetchTrade(requestJson, DefaultLeague);
                    List<float> prices = new List<float>();
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

                    Logger.Log.Info($"Data loaded for {GetType()}");
                    updateTimer.Interval = 3600 * 1000;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"{GetType()} {ex}");
                    updateTimer.Interval = 60 * 1000;
                }
            });
        }

        #region публичные методы

        public override (string Name, string Caregory) ItemSearch(string search)
        {
            var request = search.ToLower();
            var regex = new Regex($@"^{request.Replace(" ", @"\D*")}\D*");
            lock (itemsDataLocker)
            {
                foreach (var itemsDataByCategory in _itemsData)
                {
                    var item = itemsDataByCategory.Value.FirstOrDefault(o => regex.IsMatch(o.ToLower()));
                    if (item != null)
                    {
                        return (item, itemsDataByCategory.Key);
                    }
                }
            }

            regex = new Regex($@"{request.Replace(" ", @"\D*")}\D*");
            lock (itemsDataLocker)
            {
                foreach (var itemsDataByCategory in _itemsData)
                {
                    var item = itemsDataByCategory.Value.FirstOrDefault(o => regex.IsMatch(o.ToLower()));
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
            var pattern = search.ToLower().Replace("the ", @"the\s").Replace(" ", @"\D*\s\D*");
            var regex = new Regex($@"{pattern}\D*");
            lock (itemsDataLocker)
            {
                return _itemsData.SelectMany(x => x.Value)
                    .Where(i => regex.IsMatch(i.ToLower())).ToArray();
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

            try
            {
                var result = FetchTrade(requestJson, league);
                List<float> prices = new List<float>();
                foreach (var token in result)
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

                if (prices.Count > 0)
                {
                    var min = prices[0];
                    var median = prices[prices.Count / 2];
                    var mean = prices.Sum() / prices.Count;

                    var tradeLink = string.Empty;
                    try
                    {
                        var redirectSource = "https://www.pathofexile.com/api/trade/search/" +
                                        league +
                                        "?redirect&source=" +
                                        JsonConvert.SerializeObject(requestJson);
                        var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(redirectSource);
                        var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                        tradeLink = myHttpWebResponse.ResponseUri.ToString().Replace(" ", "%20");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error($"{GetType()} {ex}");
                    }

                    var priceData = new PriceData(min, median, mean, mean / ExaltedPrice, tradeLink);
                    return priceData;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType()} {ex}");
                return null;
            }
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

        private JArray Leagues()
        {
            return JArray.Parse(Common.GetContent("https://www.pathofexile.com/api/leagues"));
        }

        private JArray Characters(string account)
        {
            return JArray.Parse(
                Common.GetContent(
                    $"https://www.pathofexile.com/character-window/get-characters?accountName={account}"));
        }

        private JObject Account(string character)
        {
            return JObject.Parse(Common.GetContent(
                $"https://www.pathofexile.com/character-window/get-account-name-by-character?character={character}"));
        }

        private JArray FetchTrade(object requestJson, string league, int resultsNumber = 10)
        {
            try
            {
                var request =
                    (HttpWebRequest)WebRequest.Create($"https://www.pathofexile.com/api/trade/search/{league}");
                var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestJson));
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = data.Length;
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

                var jo = JObject.Parse(responseString);
                var id = jo["id"].Value<string>();
                var result = jo["result"].Value<JArray>();
                var results = string.Join(",", result.Take(resultsNumber).Select(t => t.Value<string>().Trim('"')));

                jo = JObject.Parse(
                    Common.GetContent($"https://www.pathofexile.com/api/trade/fetch/{results}?query={id}"));

                return jo["result"].Value<JArray>();
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType()} {ex}");
                return null;
            }
        }

        #endregion
    }
}