using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace Poebot
{
    public class Poebot
    {
        private string[] labLayouts = { "normal", "cruel", "merciless", "uber" };
        private string[] hints = { "delve", "incursion", "betrayal", /*"all"*/ };
        private Regex commandReg = new Regex(@"^[/]\S+");
        private readonly object requestLocker = new object();
        private readonly object screenshotLocker = new object();
        private readonly Poewatch poewatch;

        public Poebot(Poewatch poewatch, bool russianLang = true)
        {
            this.poewatch = poewatch;
        }

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
                if (param == string.Empty && command != "help" && command != "start") command = "err";
                return botCommand(command, param);
            }
            else
            {
                if (Regex.IsMatch(request, @"www.reddit.com/r/\S+"))
                {
                    return sendRedditImage(request);
                }
                if (Regex.IsMatch(request, @"pastebin[.]com/\S+"))
                {
                    return sendPobPartyLink(request);
                }
                return null;
            }
        }

        public string GetItemName(string search)
        {
            search = search.ToLower();
            JArray result = null;
            try
            {
                if (search[0] > 191)
                    result = JArray.Parse(Common.GetContent("https://pathofexile-ru.gamepedia.com/api.php?action=opensearch&search=" + search.Replace(" ", "+")));
                else
                    result = JArray.Parse(Common.GetContent("https://pathofexile.gamepedia.com/api.php?action=opensearch&search=" + search.Replace(" ", "+")));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return string.Empty;
            }
            if (result[3].Count() != 0)
            {
                return result[1][0].ToString();
            }
            else
            {
                Regex regex = new Regex(@"^" + search.Replace(" ", @"\D*") + @"\D*");
                Regex theRegex = new Regex(@"^the " + search.Replace(" ", @"\D*") + @"\D*");
                var item = poewatch.FirstOrDefault(o => regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower()));
                if (item != null) return item["name"].Value<string>();
                else return string.Empty;
            }
        }

        #region специальные внутренние методы
        private JArray wikiOpensearch(string search, bool russianLang = false)
        {
            return JArray.Parse(Common.GetContent("https://pathofexile" + (russianLang ? "-ru" : "") + ".gamepedia.com/api.php?action=opensearch&search=" + search.Replace(' ', '+')));
        }
        #endregion

        #region методы команд бота
        private Message botCommand(string command, string param)
        {
            string err_resp = "Неопознанный синтаксис команды. Смотри список доступных команд в описании бота или используй команду /help";
            switch (command)
            {
                case "start":
                    {
                        return new Message("Привет, я информационный бот-помощник по игре Path of Exile. Могу выдавать разную полезную информацию или присылать новости. Используй комманду /help, чтобы увидеть список всех команд");
                    }
                case "w":
                    {
                        return wikiSearch(param);
                    }
                case "p":
                    {
                        return tradeSearch(param);
                    }
                case "c":
                    {
                        return getCharInfo(param);
                    }
                case "cl":
                    {
                        return getCharList(param);
                    }
                case "b":
                    {
                        return poeNinjaBuilds(param);
                    }
                case "err":
                    {
                        return new Message(err_resp);
                    }
                case "i":
                    {
                        return wikiScreenshot(param);
                    }
                case "l":
                    {
                        return labLayout(param);
                    }
                case "h":
                    {
                        return leagueHint(param);
                    }
                case "top":
                    {
                        return topPrices(param);
                    }
                case "hm":
                    {
                        return helpMe(param);
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
"\n\nЗапрос можно писать сокращено, если не указано обратное (например /p xo hea вместо /p Xoph's Heart). Команды /p, /l, /h, /top и /hm работают только с запросами на английском языке, все остальные также понимают русский"
                        );
                    }
                case "sub":
                    {
                        return subToRss(param);
                    }
                default:
                    {
                        return new Message(err_resp);
                    }
            }
        }

        private Message tradeSearch(string srch)
        {
            srch = srch.ToLower();

            string links = Regex.Match(Regex.Match(srch, @"(5l|6l)").ToString(), @"\d").ToString();
            srch = srch.Replace("6l", "").Replace("5l", "").TrimEnd(' ');

            string league = poewatch.DefaultLeague;
            if (srch.IndexOf('|') > 0)
            {
                JArray leaguesja;
                try
                {
                    leaguesja = poewatch.Leagues();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return new Message("В данный момент сервер с базой данных недоступен");
                }
                string leagues = "";
                foreach (var el in leaguesja)
                    leagues += el["name"].Value<string>() + "\n";
                string LN = "";
                try
                {
                    LN = srch.Substring(srch.IndexOf('|') + 1).TrimEnd(' ').TrimStart(' ');
                    if (LN == "")
                        throw new Exception();
                    srch = srch.Substring(0, srch.IndexOf('|') - 1);
                    Regex Lreg = new Regex(@"^" + LN.Replace(" ", @"\S*\s?") + @"\S*");
                    league = leaguesja.FirstOrDefault(o => Lreg.IsMatch(o["name"].Value<string>().ToLower()))["name"].Value<string>();
                }
                catch
                {
                    return new Message("Некорректный ключ лиги. Список доступных лиг:\n" + leagues);
                }
            }

            string pattern = srch.Replace("the ", @"the\s");
            Regex regex = new Regex(@"^" + pattern.Replace(" ", @"\D*\s\D*") + @"\D*");
            Regex theRegex = new Regex(@"^the " + pattern.Replace(" ", @"\D*\s\D*") + @"\D*");
            JObject jo = new JObject();
            JArray ja = new JArray();

            var tmp = poewatch.Where(o => (regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower())) && o["linkCount"]?.Value<string>() == (links == "" ? null : links) && (o["variation"] == null || o["variation"].Value<string>() == "1 socket") && poewatch.TradeCategories.Contains(o["category"].Value<string>()));
            if (tmp == null || tmp.Count() == 0) return new Message("По запросу \"" + srch + "\"" + (links != "" ? " " + links + "L" : "") + " не удалось получить данные о ценах");
            foreach (var token in tmp)
            {
                try
                {
                    jo = poewatch.Item(token["id"].Value<string>());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return new Message("В данный момент сервер с базой данных недоступен");
                }
                ja = jo["leagues"].Value<JArray>();
                if (ja.Children<JObject>().FirstOrDefault(o => o["name"].Value<string>() == poewatch.DefaultLeague) != null) break;
            }

            string name = (string)jo["name"];

            if (name == "Skin of the Lords" || name == "Skin of the Loyal" || name == "Tabula Rasa" || name == "Shadowstitch") //Все 6л по умолчанию сюда
                jo = poewatch.FirstOrDefault(o => (regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower())) && o["linkCount"].Value<int>() == 6);


            string poetrade = "http://poe.trade/search?league=" + league.Replace(' ', '+') + "&online=x&name=" + name.Replace(' ', '+') + (links != "" ? "&link_min=" + links : "")/* + (corrupted ? "&corrupted=1" : "")*/;

            JArray history = new JArray();
            lock (requestLocker)
            {
                try
                {
                    history = poewatch.ItemHistory(jo["id"].Value<string>(), league);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return new Message("В данный момент сервер с базой данных недоступен");
                }
            }

            lock (requestLocker)
            {
                try
                {
                    jo = poewatch.Item(jo["id"].Value<string>());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return new Message("В данный момент сервер с базой данных недоступен");
                }
            }

            ja = JArray.Parse(jo["leagues"].ToString());
            jo = ja.Children<JObject>().FirstOrDefault(o => o["name"].Value<string>() == league);
            if (jo == null) return new Message("О предмете " + name + " на лиге " + league + " нет данных о цене");

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
                "Цены на " + name + (links != "" ? " " + links + "L" : "") + " (лига " + league + ")"
                + "\nМинимальная: " + Regex.Match((string)jo["min"], @"\d+[.]?\d{0,2}").ToString() + "c"
                + "\nСредняя: " + Regex.Match((string)jo["median"], @"\d+[.]?\d{0,2}").ToString() + "c"
                + " (" + Regex.Match((string)jo["exalted"], @"\d+[.]?\d{0,2}").ToString() + "ex)\nСсылка на трейд: " + poetrade,
                plotBytes
            );
        }

        private Message helpMe(string req)
        {
            string pattern = req.Replace("the ", @"the\s");
            Regex regex = new Regex(pattern.Replace(" ", @"\D*\s\D*") + @"\D*");
            var items = poewatch.Where(o => regex.IsMatch(o["name"].Value<string>().ToLower()) && poewatch.TradeCategories.Contains(o["category"].Value<string>()));
            var searchResults = items.Select(o => o["name"].Value<string>()).Distinct();
            if (searchResults.Count() > 30) return new Message("Найдено слишком много возможных вариантов. Уточните запрос");
            if (searchResults.Count() == 0) return new Message("По запросу " + req + " ничего не найдено");
            return new Message("Возможно вы искали:\n" + string.Join("\n", searchResults));
        }

        private Message poeNinjaBuilds(string srch)
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
                Regex regex = new Regex(@"^" + item.Replace(" ", @"\D*") + @"\D*");
                Regex theRegex = new Regex(@"^the " + item.Replace(" ", @"\D*") + @"\D*");
                JObject jo = new JObject();
                jo = poewatch.FirstOrDefault(o => (regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower())));
                string result = "", category = "";
                if (jo == null)
                {
                    try
                    {
                        result = wikiOpensearch(item)[1][0].Value<string>();
                    }
                    catch
                    {
                        return new Message("Не удалось определить \"" + item + "\"");
                    }
                    category = "keystone";
                }
                else
                {
                    result = jo["name"].ToString();
                    category = jo["category"].ToString();
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

        private Message wikiSearch(string search)
        {
            search = search.ToLower();
            string url;
            JArray result = null;
            try
            {
                result = wikiOpensearch(search, search[0] > 191);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new Message("В данный момент сервер с базой данных недоступен");
            }
            if (result[3].Count() != 0)
            {
                url = result[3][0].ToString();
            }
            else
            {
                Regex regex = new Regex(@"^" + search.Replace(" ", @"\D*\s\D*") + @"\D*");
                Regex theRegex = new Regex(@"^the " + search.Replace(" ", @"\D*\s\D*") + @"\D*");
                JObject item = poewatch.FirstOrDefault(o => (regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower())));
                if (item != null)
                {
                    url = "https://pathofexile.gamepedia.com/" + item["name"].Value<string>().Replace(' ', '_');
                }
                else return new Message("По запросу \"" + search + "\" ничего не найдено");
            }
            return new Message(url);
        }

        private Message wikiScreenshot(string search)
        {
            search = search.ToLower();
            string name = "";
            string url = "";
            JArray result = null;
            try
            {
                result = wikiOpensearch(search, search[0] > 191);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message); return new Message("В данный момент сервер с базой данных недоступен");
            }
            if (result[3].Count() != 0)
            {
                url = result[3][0].ToString();
                name = result[1][0].ToString();
            }
            else
            {
                Regex regex = new Regex(@"^" + search.Replace(" ", @"\D*") + @"\D*");
                Regex theRegex = new Regex(@"^the " + search.Replace(" ", @"\D*") + @"\D*");
                JObject item = poewatch.FirstOrDefault(o => (regex.IsMatch(o["name"].Value<string>().ToLower()) || theRegex.IsMatch(o["name"].Value<string>().ToLower())));
                if (item != null)
                {
                    name = item["name"].Value<string>();
                    url = "https://pathofexile.gamepedia.com/" + name.Replace(' ', '_');
                }
                else return new Message("По запросу \"" + search + "\" ничего не найдено");
            }

            using (var memstream = new MemoryStream())
            {
                lock (screenshotLocker)
                {
                    try
                    {
                        url = url.Replace("'", "%27");
                        Process.Start("/bin/bash", "-c \"" + $"python /home/pi/.local/bin/webscreenshot -o /tmp/screenshots -f bmp {url}" + "\"").WaitForExit();
                        string fileName = $"/tmp/screenshots/{ Regex.Replace(url.Replace(".gamepedia.com", ".gamepedia.com:443").Replace("://", "_"), @"[^\w.-]", "_") }.bmp";
                        using (Bitmap bmp = new Bitmap(fileName))
                        {
                            Color clr;
                            int x = bmp.Width - 150, y = 300, height = 0, wigth = 0;
                            for (; y < bmp.Height; y++)
                            {
                                clr = bmp.GetPixel(x, y);
                                if (clr == Color.FromArgb(255, 0, 0, 0))
                                {
                                    y--;
                                    Color borderClr = bmp.GetPixel(x, y);
                                    while (bmp.GetPixel(x - 1, y) == borderClr) x--;
                                    while (bmp.GetPixel(x + wigth, y) == borderClr) wigth++;
                                    while (bmp.GetPixel(x, y + height) == borderClr) height++;
                                    break;
                                }
                            }
                            Rectangle corp = new Rectangle(x, y, wigth, height);
                            bmp.Clone(corp, bmp.PixelFormat).Save(memstream, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return new Message("Не удалось вывести изображение этой статьи");
                    }
                }
                return new Message(image: memstream.ToArray(), sysInfo: name.Replace(' ', '-').Replace("'", "").ToLower());
            }
        }
        private Message labLayout(string search)
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
                var layouturl = "https://www.poelab.com/wp-content/labfiles/"
                    + date1.Year + "-" + string.Format("{0:00}", date1.Month) + "-" + string.Format("{0:00}", date1.Day) + "_" + search + ".jpg";
                try
                {
                    byte[] data = wc.DownloadData(layouturl);
                    return new Message(image: data);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return new Message("В данный момент сервис недоступен");
                }
            }
        }

        private Message leagueHint(string request)
        {
            string hint = hints.FirstOrDefault(o => Regex.IsMatch(o, @"^" + request.ToLower()));
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
                        return new Message("Не удалось вывести подсказку по запросу " + request +
                            "\nПодсказки доступны по запросам: " + string.Join(", ", hints));
                    }
            }
        }

        private Message topPrices(string request)
        {
            string formatHint = "Формат сообщения: category [quantity] [group]\nПо умолчанию quantity = 10, группы все";
            var split = request.Split(' ');
            if (!poewatch.TradeCategories.Contains(split[0])) return new Message("Некорректная категория. Список доступных категорий:\n" + string.Join("\n", poewatch.TradeCategories));
            int num = 10;
            string group = "";
            switch (split.Count())
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
            var results = ja.Children<JObject>().Where(o => (group != "" ? o["group"].Value<string>() == group : true) && (split[0] == "gem" ? (o["gemLevel"].Value<int>() == 20 && (string)o["gemQuality"] == "20" && o["gemIsCorrupted"].Value<bool>() == false) : true) && o["linkCount"]?.Value<string>() == null);
            if (results.Count() == 0) return new Message("Неверно задана группа. Список доступных групп для данной категории:\n" + string.Join("\n", ja.Children<JObject>().Select(o => o["group"].ToString()).Distinct()));
            return new Message("Топ " + num + " предметов по цене в категории " + split[0] + (group != "" ? " группы " + group + " " : "") + ":\n" + string.Join("\n", results.ToList().GetRange(0, num > results.Count() ? results.Count() : num).Select(o => Regex.Match(o["median"].Value<string>(), @"\d+[.]?\d{0,2}") + "c - " + o["name"].Value<string>())));
        }

        private Message getCharInfo(string charName)
        {
            JArray ja;
            try
            {
                ja = poewatch.Accounts(charName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new Message("В данный момент сервер с базой данных недоступен");
            }
            string account = "";
            try
            {
                account = (string)ja[0]["account"];
            }
            catch
            {
                return new Message("Указанный герой не найден");
            }
            ja = poewatch.Characters(account);
            charName = ja.FirstOrDefault(o => o["character"].Value<string>().ToLower() == charName.ToLower())["character"].Value<string>();
            return new Message("http://poe-profile.info/profile/" + account + "/" + charName);
        }

        private Message getCharList(string account)
        {
            JArray ja;
            try
            {
                ja = poewatch.Characters(account);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new Message("В данный момент сервер с базой данных недоступен");
            }
            if (ja.Count == 0)
                return new Message("Указанный профиль не найден");
            string chars = "";
            foreach (var jt in ja)
                chars += "\n" + jt["character"].Value<string>() + " (лига: " + jt["league"].Value<string>() + ")";
            return new Message("Список доступных для отображения персонажей профиля " + account + ":\n" + chars);
        }

        private Message subToRss(string prs)
        {
            string[] parameters = prs.Split('+');
            if (Regex.IsMatch(parameters[0], @"(ru|en)"))
            {
                List<string> subs = new List<string>();
                try
                {
                    subs = File.ReadAllLines(parameters[2]).ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return new Message("Ошибка подписки, попробуйте повторить позже");
                }
                string thisSub = subs.FirstOrDefault(x => Regex.IsMatch(x, parameters[1] + " " + parameters[0]));
                if (thisSub == null)
                {
                    using (var sw = new StreamWriter(parameters[2], true, Encoding.Default))
                    {
                        sw.WriteLine("{0} {1}", parameters[1], parameters[0]);
                    }
                    return new Message("Эта беседа подписана на новости с " + (parameters[0] == "ru" ? "русского" : "английского") + " сайта игры");
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
                        return new Message("Эта беседа отписана от новостей с " + (parameters[0] == "ru" ? "русского" : "английского") + " сайта игры");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
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

        #region методы автоответа на ссылки
        private Message sendRedditImage(string url)
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
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private Message sendPobPartyLink(string url)
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
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                var jo = JObject.Parse(responseString);
                return new Message(text: "https://pob.party/share/" + jo["url"].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        #endregion
    }
}
