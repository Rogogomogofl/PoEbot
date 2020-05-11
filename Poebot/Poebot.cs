using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Bot
{
    public class Poebot
    {
        private readonly string[] labLayouts = { "normal", "cruel", "merciless", "uber" };
        private readonly string[] hints = { "delve", "incursion", "betrayal", /*"all"*/ };
        private readonly Regex commandReg = new Regex(@"^[/]\S+");
        private readonly object requestLocker = new object();
        private readonly object screenshotLocker = new object();
        private readonly Poewatch poewatch;

        public Poebot(Poewatch poewatch, string language = "ru")
        {
            this.poewatch = poewatch;
        }

        #region внешние методы
        public Message ProcessRequest(string request)
        {
            if (!poewatch.IsDataLoaded())
            {
                return new Message("В данный момент сервер с базой данных недоступен");
            }

            if (commandReg.IsMatch(request))
            {
                string command = commandReg.Match(request).Value.TrimStart('/').ToLower();
                string param = commandReg.Split(request)[1].Trim(' ');
                if (string.IsNullOrEmpty(param) && command != "help" && command != "start") command = "err";
                return BotCommand(command, param);
            }
            else
            {
                if (Regex.IsMatch(request, @"www.reddit.com/r/\S+"))
                {
                    return SendRedditImage(request);
                }
                if (Regex.IsMatch(request, @"pastebin[.]com/\S+"))
                {
                    return SendPobPartyLink(request);
                }
                return null;
            }
        }

        public string GetItemName(string search)
        {
            search = search.ToLower();
            JArray result;
            try
            {
                if (search[0] > 191)
                    result = JArray.Parse(Common.GetContent("https://pathofexile-ru.gamepedia.com/api.php?action=opensearch&search=" + search.Replace(" ", "+")));
                else
                    result = JArray.Parse(Common.GetContent("https://pathofexile.gamepedia.com/api.php?action=opensearch&search=" + search.Replace(" ", "+")));
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                return string.Empty;
            }
            if (result[3].Any())
            {
                return result[1][0].ToString();
            }
            else
            {
                Regex regex = new Regex($@"^{search.Replace(" ", @"\D*")}\D*");
                Regex theRegex = new Regex($@"^the {search.Replace(" ", @"\D*")}\D*");
                var item = poewatch.FirstOrDefault(o => regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower()));
                if (item != null) return item["name"].Value<string>();
                else return string.Empty;
            }
        }
        #endregion

        #region специальные внутренние методы
        private JArray WikiOpensearch(string search, bool russianLang = false)
        {
            return JArray.Parse(Common.GetContent($"https://pathofexile{(russianLang ? "-ru" : "")}.gamepedia.com/api.php?action=opensearch&search={search.Replace(' ', '+')}"));
        }
        #endregion

        #region методы команд бота
        private Message BotCommand(string command, string param)
        {
            string errResp = "Неопознанный синтаксис команды. Смотри список доступных команд в описании бота или используй команду /help";
            switch (command)
            {
                case "start":
                    {
                        return new Message("Привет, я информационный бот-помощник по игре Path of Exile. Могу выдавать разную полезную информацию или присылать новости. Используй комманду /help, чтобы увидеть список всех команд");
                    }
                case "w":
                    {
                        return new Message(text: ItemSearch(param).url);
                    }
                case "p":
                    {
                        return TradeSearch(param);
                    }
                case "c":
                    {
                        return GetCharInfo(param);
                    }
                case "cl":
                    {
                        return GetCharList(param);
                    }
                case "b":
                    {
                        return PoeninjaBuilds(param);
                    }
                case "err":
                    {
                        return new Message(errResp);
                    }
                case "i":
                    {
                        return WikiScreenshot(param);
                    }
                case "l":
                    {
                        return LabLayout(param);
                    }
                case "h":
                    {
                        return LeagueHint(param);
                    }
                case "top":
                    {
                        return TopPrices(param);
                    }
                case "hm":
                    {
                        return HelpMe(param);
                    }
                case "help":
                    {
                        return new Message("Список доступных команд:" +
"\n/w название - Ссылка на wiki по запросу" +
"\n/p предмет [6l или 5l] [| лига] - Цена предмета с графиком изменения. Опционально 5 или 6 линков и выбор лиги" +
"\n/c имя героя полностью - Ссылка на профиль героя" +
"\n/cl имя профиля полностью - Вывод всех персонажей профиля" +
"\n/b предмет, камень умения или значимое пассивное умение - Вывод билдов на ниндзе с вещами из запроса. Можно указывать несколько вещей, разделенных знаком +" +
"\n/i название - Скрин с wiki по запросу (работает медленно, проявите терпение:))" +
"\n/l название или номер лабиринта - Вывод картинки с лайаутом выбранного лабиринта на сегодня" +
"\n/h название лиги - Вывод картинки с подсказками к лиге" +
"\n/sub en или /sub ru - Подписка беседы на новости на соответствующем языке" +
"\n/top категория [количество] [группа] - Вывод топа предметов по цене из указанной категории. По умолчанию количество = 10, группы все" +
"\n/hm название - Подсказка всех предметов по названию" +
"\n\nЗапрос можно писать сокращено, если не указано обратное (например /p xo hea вместо /p Xoph's Heart). Команды /p, /b, /l, /h, /top и /hm работают только с запросами на английском языке, все остальные также понимают русский"
                        );
                    }
                case "sub":
                    {
                        return SubToRss(param);
                    }
                default:
                    {
                        return new Message(errResp);
                    }
            }
        }

        private Message TradeSearch(string srch)
        {
            srch = srch.ToLower();

            string links = Regex.Match(Regex.Match(srch, @"(5l|6l)").ToString(), @"\d").ToString();
            srch = srch.Replace("6l", "").Replace("5l", "");

            string league = poewatch.DefaultLeague;
            if (srch.IndexOf('|') > 0)
            {
                JArray leaguesja;
                try
                {
                    leaguesja = Poewatch.Leagues();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                    return new Message("В данный момент сервер с базой данных недоступен");
                }
                string leagues = "";
                foreach (var el in leaguesja)
                    leagues += el["name"].Value<string>() + "\n";
                try
                {
                    var ln = srch.Substring(srch.IndexOf('|') + 1).Trim(' ');
                    if (string.IsNullOrEmpty(ln))
                        throw new Exception();
                    srch = srch.Substring(0, srch.IndexOf('|') - 1);
                    Regex lreg = new Regex($@"^{ln.Replace(" ", @"\S*\s?")}\S*");
                    league = leaguesja.FirstOrDefault(o => lreg.IsMatch(o["name"].Value<string>().ToLower()))["name"].Value<string>();
                }
                catch
                {
                    return new Message("Некорректный ключ лиги. Список доступных лиг:\n" + leagues);
                }
            }

            srch = srch.Trim(' ');

            string pattern = srch.Replace("the ", @"the\s");
            Regex regex = new Regex($@"^{pattern.Replace(" ", @"\D*\s\D*")}\D*");
            Regex theRegex = new Regex($@"^the {pattern.Replace(" ", @"\D*\s\D*")}\D*");
            JObject jo = new JObject();
            JArray ja;

            var tmp = poewatch.Where(o => (regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower()))
                                            && o["linkCount"]?.Value<string>() == (string.IsNullOrEmpty(links) ? null : links)
                                            && (o["variation"] == null || o["variation"].Value<string>() == "1 socket")
                                            && Poewatch.TradeCategories.Contains(o["category"].Value<string>()));
            if (!tmp?.Any() ?? true) return new Message($"По запросу \"{srch}\"{(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")} не удалось получить данные о ценах");
            foreach (var token in tmp)
            {
                try
                {
                    jo = Poewatch.Item(token["id"].Value<string>());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                    return new Message("В данный момент сервер с базой данных недоступен");
                }
                ja = jo["leagues"].Value<JArray>();
                if (ja.Children<JObject>().FirstOrDefault(o => o["name"].Value<string>() == poewatch.DefaultLeague) != null) break;
            }

            string name = (string)jo["name"];

            if (name == "Skin of the Lords" || name == "Skin of the Loyal" || name == "Tabula Rasa" || name == "Shadowstitch") //Все 6л по умолчанию сюда
                jo = poewatch.FirstOrDefault(o => (regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower())) && o["linkCount"].Value<int>() == 6);

            //string tradelink = "http://poe.trade/search?league=" + league.Replace(' ', '+') + "&online=x&name=" + name.Replace(' ', '+') + (!string.IsNullOrEmpty(links) ? "&link_min=" + links : "")/* + (corrupted ? "&corrupted=1" : "")*/;
            var linksquery = "\"filters\":{\"type_filters\":{\"filters\":{}},\"socket_filters\":{\"filters\":{\"links\":{\"min\":" + links + ",\"max\":" + links + "}}}},";
            var tradelink = "https://www.pathofexile.com/api/trade/search/" + league + "?redirect&source={\"query\":{" + (string.IsNullOrEmpty(links) ? string.Empty : linksquery) + "\"name\":\"" + name + "\"}}";
            try
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(tradelink);
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                tradelink = myHttpWebResponse.ResponseUri.ToString().Replace(" ", "%20");
            }
            catch
            {
                tradelink = "https://www.pathofexile.com/api/trade/search/" + league + "?redirect&source={\"query\":{\"type\":\"" + name + "\"}}";
                try
                {
                    HttpWebRequest myHttpWebRequest = (HttpWebRequest) WebRequest.Create(tradelink);
                    HttpWebResponse myHttpWebResponse = (HttpWebResponse) myHttpWebRequest.GetResponse();
                    tradelink = myHttpWebResponse.ResponseUri.ToString().Replace(" ", "%20");
                }
                catch
                {
                    tradelink = string.Empty;
                }
            }

            JArray history;
            lock (requestLocker)
            {
                try
                {
                    history = Poewatch.ItemHistory(jo["id"].Value<string>(), league);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                    return new Message("В данный момент сервер с базой данных недоступен");
                }
            }

            lock (requestLocker)
            {
                try
                {
                    jo = Poewatch.Item(jo["id"].Value<string>());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                    return new Message("В данный момент сервер с базой данных недоступен");
                }
            }

            ja = JArray.Parse(jo["leagues"].ToString());
            jo = ja.Children<JObject>().FirstOrDefault(o => o["name"].Value<string>() == league);
            if (jo == null) return new Message($"О предмете {name} на лиге {league} нет данных о цене");

            byte[] plotBytes = null;
            if (history.Count != 0)
            {
                var series = new LineSeries();
                foreach (var ele in history)
                {
                    var median = float.Parse(ele["median"].ToString());
                    var date = ele["time"].ToString().Substring(0, ele["time"].ToString().IndexOf(' '));
                    series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(Convert.ToDateTime(date)), median));
                }
                PlotModel plot = new PlotModel();
                plot.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    Angle = 45,
                    StringFormat = "dd/MM/yyyy",
                    MajorGridlineThickness = 1,
                    MajorGridlineColor = OxyColor.FromRgb(211, 211, 211),
                    MajorGridlineStyle = LineStyle.Dash,
                    CropGridlines = true
                });
                plot.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Цена в хаосах",
                    MajorGridlineThickness = 1,
                    MajorGridlineColor = OxyColor.FromRgb(211, 211, 211),
                    MajorGridlineStyle = LineStyle.Dash,
                    MinorGridlineThickness = 1,
                    MinorGridlineColor = OxyColor.FromRgb(211, 211, 211),
                    MinorGridlineStyle = LineStyle.Dash,
                    CropGridlines = true
                });
                plot.Series.Add(series);
                using (var memstream = new MemoryStream())
                {
                    var pngExporter = new PngExporter { Width = 1000, Height = 400, Background = OxyColors.White };
                    pngExporter.Export(plot, memstream);
                    plotBytes = memstream.ToArray();
                }
            }
            return new Message
            (
                $"Цены на {name} {(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")} (лига {league})"
                + $"\nМинимальная: {Regex.Match((string)jo["min"], @"\d+[.]?\d{0,2}")}c"
                + $"\nСредняя: {Regex.Match((string)jo["median"], @"\d+[.]?\d{0,2}")}c"
                + $" ({Regex.Match((string)jo["exalted"], @"\d+[.]?\d{0,2}")}ex)\nСсылка на трейд: {tradelink}",
                plotBytes
            );
        }

        private Message HelpMe(string req)
        {
            string pattern = req.Replace("the ", @"the\s");
            Regex regex = new Regex(pattern.Replace(" ", @"\D*\s\D*") + @"\D*");
            var items = poewatch.Where(o => regex.IsMatch(o["name"].Value<string>().ToLower())
                                            && Poewatch.TradeCategories.Contains(o["category"].Value<string>()));
            var searchResults = items.Select(o => o["name"].Value<string>()).Distinct();
            if (searchResults.Count() > 30) return new Message("Найдено слишком много возможных вариантов. Уточните запрос");
            if (!searchResults.Any()) return new Message($"По запросу {req} ничего не найдено");
            return new Message($"Возможно вы искали:\n{string.Join("\n", searchResults)}");
        }

        private Message PoeninjaBuilds(string srch)
        {
            List<string> items = new List<string>();
            while (srch.IndexOf('+') > 0)
            {
                items.Add(srch.Substring(0, srch.IndexOf('+')).TrimStart(' ').TrimEnd(' '));
                srch = srch.Substring(srch.IndexOf('+') + 1);
            }
            items.Add(srch.TrimStart(' ').TrimEnd(' '));

            List<string> uniques = new List<string>();
            List<string> skills = new List<string>();
            List<string> keystones = new List<string>();
            foreach (var item in items)
            {
                Regex regex = new Regex($@"^{item.Replace(" ", @"\D*")}\D*");
                Regex theRegex = new Regex($@"^the {item.Replace(" ", @"\D*")}\D*");
                JObject jo = poewatch.FirstOrDefault(o => (regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower()))
                                                        && o["category"].Value<string>() != "enchantment");
                string result, category;
                if (jo == null)
                {
                    try
                    {
                        result = WikiOpensearch(item)[1][0].Value<string>();
                    }
                    catch
                    {
                        return new Message($"Не удалось определить \"{item}\"");
                    }
                    category = "keystone";
                }
                else
                {
                    result = jo["name"].Value<string>();
                    category = jo["category"].Value<string>();
                }

                switch (category)
                {
                    case "weapon":
                        {
                            uniques.Add(result);
                            break;
                        }
                    case "accessory":
                        {
                            uniques.Add(result);
                            break;
                        }
                    case "armour":
                        {
                            uniques.Add(result);
                            break;
                        }
                    case "jewel":
                        {
                            uniques.Add(result);
                            break;
                        }
                    case "flask":
                        {
                            uniques.Add(result);
                            break;
                        }
                    case "gem":
                        {
                            if (jo["group"].Value<string>() == "support") return new Message("Камни поддержки не поддерживаются этой командой");
                            skills.Add(result);
                            break;
                        }
                    case "keystone":
                        {
                            keystones.Add(result);
                            break;
                        }
                    default:
                        {
                            return new Message("Не удалось определить \"" + item + "\"");
                        }
                }
            }
            string fstRetStr = "Билды которые используют", sndRetStr = ":\nhttps://poe.ninja/challenge/builds?";
            if (uniques.Count > 0)
            {
                sndRetStr += "item=";
                foreach (var unique in uniques)
                {
                    fstRetStr += " " + unique + " +";
                    sndRetStr += unique.Replace(' ', '-') + ',';
                }
                sndRetStr = sndRetStr.Substring(0, sndRetStr.Length - 1);
            }

            if (skills.Count > 0)
            {
                sndRetStr += "&skill=";
                foreach (var skill in skills)
                {
                    fstRetStr += " " + skill + " +";
                    sndRetStr += skill.Replace(' ', '-') + ',';
                }
                sndRetStr = sndRetStr.Substring(0, sndRetStr.Length - 1);
            }

            if (keystones.Count > 0)
            {
                sndRetStr += "&keystone=";
                foreach (var keystone in keystones)
                {
                    fstRetStr += " " + keystone + " +";
                    sndRetStr += keystone.Replace(' ', '-') + ',';
                }
                sndRetStr = sndRetStr.Substring(0, sndRetStr.Length - 1);
            }
            return new Message(fstRetStr.Substring(0, fstRetStr.Length - 2) + sndRetStr);
        }

        private (string name, string url) ItemSearch(string search)
        {
            search = search.ToLower();
            string name, url;
            JArray result;
            try
            {
                result = WikiOpensearch(search, search[0] > 191);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                return ("", "В данный момент сервер с базой данных недоступен");
            }
            if (result[3].Any())
            {
                url = result[3][0].ToString();
                name = result[1][0].ToString();
            }
            else
            {
                Regex regex = new Regex($@"^{search.Replace(" ", @"\D*")}\D*");
                Regex theRegex = new Regex($@"^the {search.Replace(" ", @"\D*")}\D*");
                JObject item = poewatch.FirstOrDefault(o => (regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower())));
                if (item != null)
                {
                    name = item["name"].Value<string>();
                    url = $"https://pathofexile.gamepedia.com/{name.Replace(' ', '_')}";
                }
                else return ("", $"По запросу \"{search}\" ничего не найдено");
            }
            return (name, url);
        }

        private Message WikiScreenshot(string search)
        {
            var wiki = ItemSearch(search);
            string url = wiki.url;
            string name = wiki.name;
            if (string.IsNullOrEmpty(name)) return new Message(text: url);

            lock (screenshotLocker)
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("enable-automation");
                options.AddArgument("headless");
                options.AddArgument("no-sandbox");
                options.AddArgument("disable-infobars");
                options.AddArgument("disable-dev-shm-usage");
                options.AddArgument("disable-browser-side-navigation");
                options.AddArgument("disable-gpu");
                options.AddArgument("window-size=1000,2000");
                options.PageLoadStrategy = PageLoadStrategy.None;
                using (ChromeDriver driver = new ChromeDriver("/usr/bin", options)) //for linux
                //using (ChromeDriver driver = new ChromeDriver(options)) //for windows
                {
                    try
                    {
                        driver.Navigate().GoToUrl(url);
                        System.Threading.Thread.Sleep(4000);
                        var bytes = driver.GetScreenshot().AsByteArray;
                        IWebElement element;
                        if (poewatch.FirstOrDefault(o => o["name"].Value<string>() == name) != null)
                        {
                            element = driver.FindElementByCssSelector(".infobox-page-container");
                        }
                        else
                        {
                            element = driver.FindElementByCssSelector(".infocard");
                        }
                        using (Bitmap screenshot = new Bitmap(new MemoryStream(bytes)))
                        {
                            Rectangle croppedImage = new Rectangle(element.Location.X, element.Location.Y - 50, element.Size.Width, element.Size.Height);
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                screenshot.Clone(croppedImage, screenshot.PixelFormat).Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                                return new Message(image: memoryStream.ToArray(), sysInfo: name.Replace(' ', '-').Replace("'", "").ToLower());
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                        return new Message("Не удалось вывести изображение этой статьи");
                    }
                }
            }
        }

        private Message LabLayout(string search)
        {
            Regex regex = new Regex(@"^" + search.ToLower() + @"\S*");
            try
            {
                search = labLayouts[int.Parse(search) - 1];
            }
            catch
            {
                foreach (var layout in labLayouts)
                {
                    if (regex.IsMatch(layout))
                    {
                        search = layout;
                        break;
                    }
                    else if (layout == labLayouts.Last())
                        return new Message("Неверно задана сложность лабиринта");
                }
            }
            using (var wc = new WebClient())
            {
                DateTime date1 = DateTime.Today;
                var layouturl = "https://www.poelab.com/wp-content/labfiles/" + $"{date1.Year}-{string.Format("{0:00}", date1.Month) + "-" + string.Format("{0:00}", date1.Day)}_{search}.jpg";
                try
                {
                    byte[] data = wc.DownloadData(layouturl);
                    return new Message(image: data);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                    return new Message("В данный момент сервис недоступен");
                }
            }
        }

        private Message LeagueHint(string request)
        {
            string hint = hints.FirstOrDefault(o => Regex.IsMatch(o, $@"^{request.ToLower()}"));
            switch (hint)
            {
                case "delve":
                    {
                        return new Message(loadedPhoto: new LoadedPhoto(vkId: 456241317, vkOwnerId: 37321011, telegramId: "AgADAgADr6wxGxVgSEs6sklltPi6ZAOUwg8ABAEAAwIAA3kAA_pSAQABFgQ"));
                    }
                case "incursion":
                    {
                        return new Message(loadedPhoto: new LoadedPhoto(vkId: 456241318, vkOwnerId: 37321011, telegramId: "AgADAgADsKwxGxVgSEu-Fwh4MWJZolzPuQ8ABAEAAwIAA3cAA91WBgABFgQ"));
                    }
                case "betrayal":
                    {
                        return new Message(loadedPhoto: new LoadedPhoto(vkId: 457242989, vkOwnerId: 37321011, telegramId: "AgADAgADtawxGxVgSEu2WI3Xvp2ohJPYuQ8ABAEAAwIAA3cAA3NNBgABFgQ"));
                    }
                /*case "all":
                    {
                        id = 456241319;
                        break;
                    }*/
                default:
                    {
                        return new Message($"Не удалось вывести подсказку по запросу {request}\nПодсказки доступны по запросам: {string.Join(", ", hints)}");
                    }
            }
        }

        private Message TopPrices(string request)
        {
            string formatHint = "Формат сообщения: category [quantity] [group]\nПо умолчанию quantity = 10, группы все";
            var split = request.Split(' ');
            if (!Poewatch.TradeCategories.Contains(split[0])) return new Message($"Некорректная категория. Список доступных категорий:\n{string.Join("\n", Poewatch.TradeCategories)}");
            int num = 10;
            string group = "";
            switch (split.Length)
            {
                case 2:
                    {
                        try
                        {
                            num = int.Parse(split[1]);
                        }
                        catch
                        {
                            group = split[1];
                        }
                        break;
                    }
                case 3:
                    {
                        if (!int.TryParse(split[1], out num)) return new Message(formatHint);
                        group = split[2];
                        break;
                    }
            }
            if (num < 1) return new Message(formatHint);
            JArray ja = poewatch.Get(split[0]);
            if (ja == null || ja.Count == 0) return new Message("Не удалось получить данные о ценах");
            var results = ja.Children<JObject>().Where(o => (!string.IsNullOrEmpty(group) ? o["group"].Value<string>() == group : true)
                                                        && (split[0] == "gem" ? (o["gemLevel"].Value<int>() == 20 && (string)o["gemQuality"] == "20" && o["gemIsCorrupted"].Value<bool>() == false) : true)
                                                        && o["linkCount"]?.Value<string>() == null);
            if (!results.Any()) return new Message($"Неверно задана группа. Список доступных групп для данной категории:\n{string.Join("\n", ja.Children<JObject>().Select(o => o["group"].ToString()).Distinct())}");
            return new Message($"Топ {num} предметов по цене в категории {split[0]}{(!string.IsNullOrEmpty(group) ? " группы " + group + " " : "")}:"
                + $"\n{string.Join("\n", results.ToList().GetRange(0, num > results.Count() ? results.Count() : num).Select(o => $"{Regex.Match(o["median"].Value<string>(), @"\d+[.]?\d{0,2}")}c - {o["name"].Value<string>()}"))}");
        }

        private Message GetCharInfo(string charName)
        {
            JArray ja;
            try
            {
                ja = Poewatch.Accounts(charName);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                return new Message("В данный момент сервер с базой данных недоступен");
            }
            string account;
            try
            {
                account = (string)ja[0]["account"];
            }
            catch
            {
                return new Message("Указанный герой не найден");
            }
            ja = Poewatch.Characters(account);
            charName = ja.FirstOrDefault(o => o["character"].Value<string>().ToLower() == charName.ToLower())["character"].Value<string>();
            return new Message("http://poe-profile.info/profile/" + $"{account}/{charName}");
        }

        private Message GetCharList(string account)
        {
            JArray ja;
            try
            {
                ja = Poewatch.Characters(account);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                return new Message("В данный момент сервер с базой данных недоступен");
            }
            if (ja.Count == 0)
                return new Message("Указанный профиль не найден");
            string chars = "";
            foreach (var jt in ja)
                chars += $"\n{jt["character"].Value<string>()} (лига: {jt["league"].Value<string>()}";
            return new Message($"Список доступных для отображения персонажей профиля {account}:\n{chars}");
        }

        private Message SubToRss(string prs)
        {
            string[] parameters = prs.Split('+');
            if (Regex.IsMatch(parameters[0], @"(ru|en)"))
            {
                List<string> subs;
                try
                {
                    subs = File.ReadAllLines(parameters[2]).ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                    return new Message("Ошибка подписки, попробуйте повторить позже");
                }
                string thisSub = subs.FirstOrDefault(x => Regex.IsMatch(x, parameters[1] + " " + parameters[0]));
                if (thisSub == null)
                {
                    using (var sw = new StreamWriter(parameters[2], true, Encoding.Default))
                    {
                        sw.WriteLine("{0} {1}", parameters[1], parameters[0]);
                    }
                    return new Message($"Эта беседа подписана на новости с {(parameters[0] == "ru" ? "русского" : "английского")} сайта игры");
                }
                else
                {
                    subs.Remove(thisSub);
                    try
                    {
                        using (var sw = new StreamWriter(parameters[2], false, Encoding.Default))
                        {
                            foreach (var line in subs)
                                sw.WriteLine(line);
                        }
                        return new Message($"Эта беседа отписана от новостей с {(parameters[0] == "ru" ? "русского" : "английского")} сайта игры");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                        return new Message("Ошибка подписки, попробуйте повторить позже");
                    }
                }
            }
            else
            {
                return new Message("Указан неверный язык подписки");
            }
        }
        #endregion

        #region методы автоответа
        private Message SendRedditImage(string url)
        {
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument hd = web.Load(url);
                string title = hd.DocumentNode.SelectSingleNode("//h1[contains(@class, '_eYtD2XCVieq6emjKBH3m')]").InnerText.Replace("&#x27;", "'");
                HtmlNode node = hd.DocumentNode.SelectSingleNode("//a[contains(@class, 'b5szba-0')]");
                if (node == null) node = hd.DocumentNode.SelectSingleNode("//img[contains(@class, '_2_tDEnGMLxpM6uOa2kaDB3')]").ParentNode;
                using (var wc = new WebClient())
                {
                    byte[] data = wc.DownloadData(node.Attributes["href"].Value);
                    return new Message(text: title, image: data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                return null;
            }
        }

        private Message SendPobPartyLink(string url)
        {
            try
            {
                url = "http://pastebin.com/raw/" + Regex.Split(url, "https://pastebin.com/")[1];
                var pobcode = Common.GetContent(url);
                var request = (HttpWebRequest)WebRequest.Create("https://pob.party/kv/put?ver=latest");
                var data = Encoding.ASCII.GetBytes(pobcode);
                request.Method = "POST";
                request.ContentType = "text/plain";
                request.ContentLength = data.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                var response = (HttpWebResponse)request.GetResponse();
                string responseString;
                using (var streamReader = new StreamReader(response.GetResponseStream())) responseString = streamReader.ReadToEnd();
                var jo = JObject.Parse(responseString);
                return new Message(text: "https://pob.party/share/" + jo["url"].Value<string>());
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                return null;
            }
        }
        #endregion
    }
}
