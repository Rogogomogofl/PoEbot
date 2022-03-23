using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BotHandlers.Abstracts;
using BotHandlers.Models;
using BotHandlers.Static;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace BotHandlers.Methods
{
    public class Poebot
    {
        private readonly string[] labLayouts = { "normal", "cruel", "merciless", "uber" };
        private readonly string[] hints = { "delve", "incursion", "betrayal", /*"all"*/};
        private readonly Regex commandReg = new Regex(@"^[/]\S+");
        private readonly AbstractApi api;
        private readonly AbstractPhoto photo;
        private readonly AbstractChatLanguage chatLanguage;

        public Poebot(AbstractApi api, AbstractPhoto photo, AbstractChatLanguage chatLanguage)
        {
            this.api = api;
            this.photo = photo;
            this.chatLanguage = chatLanguage;
        }

        #region внешние методы

        public Message ProcessRequest(string request)
        {
            if (commandReg.IsMatch(request))
            {
                var command = commandReg.Match(request).Value.TrimStart('/').ToLower();
                var param = commandReg.Split(request)[1].Trim(' ');
                if (string.IsNullOrEmpty(param) && command != "help" && command != "start") command = "err";
                return BotCommand(command, param);
            }

            if (request.Contains(@"reddit.com/r/"))
            {
                return SendRedditImage(request);
            }

            if (request.Contains(@"pastebin.com/"))
            {
                return SendPobPartyLink(request);
            }

            return null;
        }

        #endregion

        #region специальные внутренние методы

        private JArray WikiOpensearch(string search, bool russianLang = false)
        {
            return JArray.Parse(Common.GetContent(
                $"https://pathofexile{(russianLang ? "-ru" : "")}.gamepedia.com/api.php?action=opensearch&search={search.Replace(' ', '+')}"));
        }

        #endregion

        #region методы команд бота

        private Message BotCommand(string command, string param)
        {
            var errResp = ResponseDictionary.UnidentifiedCommand(chatLanguage.Language);
            return command switch
            {
                "start" => new Message(ResponseDictionary.HelloMessage(chatLanguage.Language)),
                "w" => new Message(ItemSearch(param).Url),
                "p" => TradeSearch(param),
                "c" => GetCharInfo(param),
                "cl" => GetCharList(param),
                "b" => PoeninjaBuilds(param),
                "err" => new Message(errResp),
                "i" => WikiScreenshot(param),
                "l" => LabLayout(param),
                "h" => LeagueHint(param),
                //"top" => TopPrices(param),
                "hm" => HelpMe(param),
                "help" => new Message(ResponseDictionary.HelpMessage(chatLanguage.Language)),
                "sub" => SubToRss(param),
                "lang" => ChangeResponseLanguage(param),
                _ => new Message(errResp)
            };
        }

        private Message TradeSearch(string srch)
        {
            if (!api.IsDataLoaded)
            {
                return new Message(ResponseDictionary.DatabaseUnavailable(chatLanguage.Language));
            }

            var match = Regex.Match(srch, @"( [56]l)", RegexOptions.IgnoreCase).ToString();
            var links = Regex.Match(match, @"\d", RegexOptions.IgnoreCase).ToString();
            if (!string.IsNullOrWhiteSpace(match))
            {
                srch = srch.Replace(match, "");
            }

            string league;
            var split = srch.Split('|').Select(s => s.Trim()).ToArray();
            if (split.Length > 1)
            {
                var leagues = api.GetLeagues();
                if (leagues == null)
                {
                    return new Message(ResponseDictionary.DatabaseUnavailable(chatLanguage.Language));
                }

                var ln = split.Last();
                if (string.IsNullOrEmpty(ln))
                {
                    return new Message(
                        ResponseDictionary.IncorrectLeagueKey(chatLanguage.Language, string.Join("\n", leagues)));
                }

                srch = split.First();
                var lreg = new Regex($@"^{ln.Replace(" ", @"\S*\s?")}\S*", RegexOptions.IgnoreCase);
                league = leagues.FirstOrDefault(l => lreg.IsMatch(l));
            }
            else
            {
                league = api.DefaultLeague;
            }

            var item = ItemSearch(srch);
            if (string.IsNullOrEmpty(item.Name))
            {
                return new Message(item.Url);
            }

            var priceData = api.GetPrice(item.Name, league, links);
            if (priceData == null)
            {
                return new Message(ResponseDictionary.NoPriceData(chatLanguage.Language, item.Name, league));
            }

            byte[] plotBytes = null;
            if (priceData.PriceHistory.Any())
            {
                var series = new LineSeries();
                series.Points.AddRange(priceData.PriceHistory.Select(ele => new DataPoint(DateTimeAxis.ToDouble(ele.Key), ele.Value)));

                var plot = new PlotModel();
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
                    Title = ResponseDictionary.PlotTitle(chatLanguage.Language),
                    MajorGridlineThickness = 1,
                    MajorGridlineColor = OxyColor.FromRgb(211, 211, 211),
                    MajorGridlineStyle = LineStyle.Dash,
                    MinorGridlineThickness = 1,
                    MinorGridlineColor = OxyColor.FromRgb(211, 211, 211),
                    MinorGridlineStyle = LineStyle.Dash,
                    CropGridlines = true
                });
                plot.Series.Add(series);
                using var memstream = new MemoryStream();
                var pngExporter = new PngExporter { Width = 1000, Height = 400, Background = OxyColors.White };
                pngExporter.Export(plot, memstream);
                plotBytes = memstream.ToArray();
            }

            photo.UploadPhoto(plotBytes);
            return new Message
            (
                ResponseDictionary.PriceResponce(chatLanguage.Language, item.Name, links, league, priceData),
                photo
            );
        }

        private Message HelpMe(string req)
        {
            if (!api.IsDataLoaded)
            {
                return new Message(ResponseDictionary.DatabaseUnavailable(chatLanguage.Language));
            }

            var items = api.ItemsSearch(req);
            if (items.Length > 30)
            {
                return new Message(ResponseDictionary.ToManyResults(chatLanguage.Language));
            }
            if (!items.Any())
            {
                return new Message(ResponseDictionary.NoResults(chatLanguage.Language, req));
            }
            return new Message(ResponseDictionary.PossibleVariants(chatLanguage.Language, items));
        }

        private Message PoeninjaBuilds(string srch)
        {
            if (!api.IsDataLoaded)
            {
                return new Message(ResponseDictionary.DatabaseUnavailable(chatLanguage.Language));
            }

            var items = new List<string>();
            while (srch.IndexOf('+') > 0)
            {
                items.Add(srch.Substring(0, srch.IndexOf('+')).Trim());
                srch = srch.Substring(srch.IndexOf('+') + 1);
            }

            items.Add(srch.Trim());

            var uniques = new List<string>();
            var skills = new List<string>();
            var keystones = new List<string>();
            foreach (var item in items)
            {
                var result = api.ItemSearch(item);
                string name, category;
                if (result.Name == null)
                {
                    try
                    {
                        name = WikiOpensearch(item)[1][0].Value<string>();
                    }
                    catch
                    {
                        return new Message(ResponseDictionary.CouldntDetermine(chatLanguage.Language, item));
                    }

                    category = "Keystone";
                }
                else
                {
                    name = result.Name;
                    category = result.Caregory;
                }

                switch (category)
                {
                    case "Weapons":
                        {
                            uniques.Add(name);
                            break;
                        }
                    case "Accessories":
                        {
                            uniques.Add(name);
                            break;
                        }
                    case "Armour":
                        {
                            uniques.Add(name);
                            break;
                        }
                    case "Jewels":
                        {
                            uniques.Add(name);
                            break;
                        }
                    case "Flasks":
                        {
                            uniques.Add(name);
                            break;
                        }
                    case "Gems":
                        {
                            if (name.Contains("Support"))
                            {
                                return new Message(ResponseDictionary.SupportsNotSupported(chatLanguage.Language));
                            }
                            skills.Add(name);
                            break;
                        }
                    case "Keystone":
                        {
                            keystones.Add(name);
                            break;
                        }
                    default:
                        {
                            return new Message(ResponseDictionary.CouldntDetermine(chatLanguage.Language, item));
                        }
                }
            }

            var urlSb = new StringBuilder(":\nhttps://poe.ninja/challenge/builds?");

            if (uniques.Any())
            {
                urlSb.Append($"item={string.Join(",", uniques.Select(i => i.Replace(' ', '-')))}");
            }

            if (skills.Any())
            {
                urlSb.Append($"&skill={string.Join(",", skills.Select(i => i.Replace(' ', '-')))}");
            }

            if (keystones.Any())
            {
                urlSb.Append($"&keystone={string.Join(",", keystones.Select(i => i.Replace(' ', '-')))}");
            }

            return new Message(ResponseDictionary.BuildsThatUse(chatLanguage.Language) + $" {string.Join(" + ", uniques.Concat(skills).Concat(keystones))}" + urlSb.ToString());
        }

        private (string Name, string Url) ItemSearch(string search)
        {
            string name, url;
            JArray result;
            try
            {
                result = WikiOpensearch(search, search[0] > 191);
            }
            catch (Exception ex)
            {
                Common.Logger?.LogError(ex);
                return ("", ResponseDictionary.DatabaseUnavailable(chatLanguage.Language));
            }

            if (result[3].Any())
            {
                url = result[3][0].Value<string>();
                name = result[1][0].Value<string>();
            }
            else
            {
                if (!api.IsDataLoaded)
                {
                    return ("", ResponseDictionary.DatabaseUnavailable(chatLanguage.Language));
                }

                var item = api.ItemSearch(search);
                if (item.Name != null)
                {
                    name = item.Name;
                    url = "https://pathofexile.gamepedia.com/" + name.Replace(' ', '_');
                }
                else return ("", ResponseDictionary.NoResults(chatLanguage.Language, search));
            }

            return (name, url);
        }

        private Message WikiScreenshot(string search)
        {
            var wiki = ItemSearch(search);
            var url = wiki.Url;
            var name = wiki.Name;
            if (string.IsNullOrEmpty(name)) return new Message(url);

            if (photo.LoadPhotoFromFile(name)) return new Message(photo);

            var options = new ChromeOptions();
            options.AddArgument("enable-automation");
            options.AddArgument("headless");
            options.AddArgument("no-sandbox");
            options.AddArgument("disable-infobars");
            options.AddArgument("disable-dev-shm-usage");
            options.AddArgument("disable-browser-side-navigation");
            options.AddArgument("disable-gpu");
            options.AddArgument("window-size=1000,2000");
            options.PageLoadStrategy = PageLoadStrategy.None;

            var os = Environment.OSVersion;
            string driverDirectory;
            switch (os.Platform)
            {
                case PlatformID.Win32NT:
                    {
                        driverDirectory = Environment.CurrentDirectory;
                        break;
                    }
                case PlatformID.Unix:
                    {
                        driverDirectory = "/usr/bin";
                        break;
                    }
                default:
                    {
                        return new Message(ResponseDictionary.SomethingWrong(chatLanguage.Language));
                    }
            }

            try
            {
                using var driver = new ChromeDriver(driverDirectory, options);
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(4500);
                var bytes = driver.GetScreenshot().AsByteArray;
                var normalizeHeight = true;
                IWebElement element;
                try
                {
                    element = driver.FindElementByCssSelector(".infobox-page-container");
                }
                catch
                {
                    element = driver.FindElementByCssSelector(".infocard");
                    normalizeHeight = false;
                }

                using var bytesStream = new MemoryStream(bytes);
                using var screenshot = new Bitmap(bytesStream);

                var y = element.Location.Y;
                if (normalizeHeight)
                    for (; y > 0; y--)
                    {
                        var pixel = screenshot.GetPixel(element.Location.X,
                            y + element.Size.Height - 1);

                        if (screenshot.GetPixel(element.Location.X + 1, y + element.Size.Height - 2) !=
                            Color.FromArgb(255, 0, 0, 0)) continue;
                        if (screenshot.GetPixel(element.Location.X + 1, y + element.Size.Height - 1) !=
                            pixel) continue;
                        if (screenshot.GetPixel(element.Location.X, y + element.Size.Height - 2) !=
                            pixel) continue;
                        break;
                    }

                if (y == 1) throw new Exception(ResponseDictionary.ImageFailed(chatLanguage.Language, name));

                var croppedImage = new Rectangle(element.Location.X, y,
                    element.Size.Width, element.Size.Height);
                using var memoryStream = new MemoryStream();
                screenshot.Clone(croppedImage, screenshot.PixelFormat).Save(memoryStream,
                    ImageFormat.Png);
                photo.SavePhoto(name.Replace(' ', '-').Replace("'", "").ToLower(),
                    memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                Common.Logger?.LogError(ex);
                return new Message(ResponseDictionary.ImageFailed(chatLanguage.Language, name));
            }

            return new Message(photo);
        }

        private Message LabLayout(string search)
        {
            var regex = new Regex(@"^" + search + @"\S*", RegexOptions.IgnoreCase);
            if (!int.TryParse(search, out var labNum))
            {
                for (var i = 0; i < 4; i++)
                {
                    if (regex.IsMatch(labLayouts[i]))
                    {
                        labNum = i + 1;
                    }
                }
            }
            if (labNum < 1 || labNum > 4)
            {
                return new Message(ResponseDictionary.IncorrectLabDifficulty(chatLanguage.Language));
            }

            try
            {
                var web = new HtmlWeb();
                var hd = web.Load("https://www.poelab.com/");
                var labs = hd.DocumentNode.SelectNodes("//div[@class='su-column-inner su-clearfix']");
                if (labs.Count != 4)
                {
                    return new Message(ResponseDictionary.SomethingWrong(chatLanguage.Language));
                }
                var href = labs[4 - labNum].SelectSingleNode(".//a[@href]");
                var labUrl = href.GetAttributeValue("href", string.Empty);
                var labDoc = web.Load(labUrl);
                var img = labDoc.DocumentNode.SelectSingleNode(".//img[@id='notesImg']");
                var labImageUrl = img.GetAttributeValue("src", string.Empty);

                using var wc = new WebClient();
                var data = wc.DownloadData(labImageUrl);
                photo.UploadPhoto(data);
                return new Message(photo);
            }
            catch (Exception ex)
            {
                Common.Logger?.LogError(ex);
                return new Message(ResponseDictionary.DatabaseUnavailable(chatLanguage.Language));
            }
        }

        private Message LeagueHint(string request)
        {
            var hint = hints.FirstOrDefault(o => Regex.IsMatch(o, $@"^{request}", RegexOptions.IgnoreCase));
            if (!photo.GetPresetPhoto(hint))
            {
                return new Message(ResponseDictionary.IncorrectHintKey(chatLanguage.Language, request, hints));
            }

            return new Message(photo);
        }

        /*private Message TopPrices(string request)
        {
            if (!api.IsDataLoaded)
            {
                return new Message(ResponseDictionary.DatabaseUnavailable(chatLanguage.Language));
            }
            var formatHint = ResponseDictionary.HintFormat(chatLanguage.Language);
            var split = request.Split(' ');
            if (!api.TradeCategories.Contains(split[0]))
                return new Message(ResponseDictionary.IncorrectCategory(chatLanguage.Language, api.TradeCategories));
            var num = 10;
            var group = "";
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
            var ja = api.Get(split[0]);
            if (ja == null || ja.Count == 0) return new Message(ResponseDictionary.UnableToObtainPrice(chatLanguage.Language));
            var results = ja.Children<JObject>().Where(o =>
                (!string.IsNullOrEmpty(group) ? o["group"].Value<string>() == group : true)
                && (split[0] == "gem"
                    ? (o["gemLevel"].Value<int>() == 20 && (string)o["gemQuality"] == "20" &&
                       o["gemIsCorrupted"].Value<bool>() == false)
                    : true)
                && o["linkCount"]?.Value<string>() == null);
            if (!results.Any())
                return new Message(ResponseDictionary.IncorrectGroup(chatLanguage.Language, ja));
            return new Message(ResponseDictionary.TopPricesResponce(chatLanguage.Language, num, split[0], group, results));
        }*/

        private Message GetCharInfo(string charName)
        {
            var account = api.GetAccountName(charName);
            if (account == null)
            {
                return new Message(ResponseDictionary.CharacterNotFound(chatLanguage.Language));
            }

            var characters = api.GetCharactersList(account);
            charName = characters.FirstOrDefault(c => c.Name.Equals(charName, StringComparison.OrdinalIgnoreCase)).Name;
            return new Message("http://poe-profile.info/profile/" + $"{account}/{charName}");
        }

        private Message GetCharList(string account)
        {
            var characters = api.GetCharactersList(account);
            return characters == null ?
                new Message(ResponseDictionary.ProfileNotFound(chatLanguage.Language)) :
                new Message(ResponseDictionary.CharListResponce(chatLanguage.Language, account, characters));
        }

        private Message SubToRss(string prs)
        {
            var parameters = prs.Split('+');
            if (Regex.IsMatch(parameters[0], @"(ru|en)"))
            {
                List<string> subs;
                try
                {
                    subs = File.ReadAllLines(parameters[2]).ToList();
                }
                catch (Exception ex)
                {
                    Common.Logger?.LogError(ex);
                    return new Message(ResponseDictionary.SubscriptionFailed(chatLanguage.Language));
                }

                var thisSub = subs.FirstOrDefault(x => Regex.IsMatch(x, parameters[1] + " " + parameters[0]));
                if (thisSub == null)
                {
                    using (var sw = new StreamWriter(parameters[2], true, Encoding.Default))
                    {
                        sw.WriteLine($"{parameters[1]} {parameters[0]}");
                    }

                    return new Message(ResponseDictionary.RssSubscription(chatLanguage.Language, parameters[0]));
                }

                subs.Remove(thisSub);
                try
                {
                    using (var sw = new StreamWriter(parameters[2], false, Encoding.Default))
                    {
                        foreach (var line in subs)
                            sw.WriteLine(line);
                    }

                    return new Message(ResponseDictionary.RssUnsubscription(chatLanguage.Language, parameters[0]));
                }
                catch (Exception ex)
                {
                    Common.Logger?.LogError(ex);
                    return new Message(ResponseDictionary.SubscriptionFailed(chatLanguage.Language));
                }
            }

            return new Message(ResponseDictionary.IncorrectLanguage(chatLanguage.Language));
        }

        private Message ChangeResponseLanguage(string language)
        {
            ResponseLanguage languageEnum;
            try
            {
                languageEnum = ResponseDictionary.CodeToEnum(language);
            }
            catch (Exception ex)
            {
                Common.Logger?.LogError(ex);
                return new Message(ResponseDictionary.IncorrectLanguage(chatLanguage.Language));
            }

            chatLanguage.Language = languageEnum;
            return new Message(ResponseDictionary.LanguageChanged(languageEnum));
        }

        #endregion

        #region методы автоответа

        private Message SendRedditImage(string url)
        {
            try
            {
                var web = new HtmlWeb();
                var hd = web.Load(url);
                var title = hd.DocumentNode.SelectSingleNode("//h1[contains(@class, '_eYtD2XCVieq6emjKBH3m')]")
                    .InnerText.Replace("&#x27;", "'");
                var node = hd.DocumentNode.SelectSingleNode("//a[contains(@class, '_13svhQIUZqD9PVzFcLwOKT styled-outbound-link')]");
                node ??= hd.DocumentNode.SelectSingleNode("//img[contains(@class, '_2_tDEnGMLxpM6uOa2kaDB3')]")
                    .ParentNode;
                using var wc = new WebClient();
                var data = wc.DownloadData(node.Attributes["href"].Value);
                photo.UploadPhoto(data);
                return new Message(title, photo);
            }
            catch (Exception ex)
            {
                Common.Logger?.LogError(ex);
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
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                    responseString = streamReader.ReadToEnd();
                var jo = JObject.Parse(responseString);
                return new Message("https://pob.party/share/" + jo["url"].Value<string>());
            }
            catch (Exception ex)
            {
                Common.Logger?.LogError(ex);
                return null;
            }
        }

        #endregion
    }
}