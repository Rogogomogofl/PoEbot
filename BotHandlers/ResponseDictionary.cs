using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace BotHandlers
{
    public enum ResponceLanguage
    {
        Russain,
        English
    }

    public static class ResponseDictionary
    {
        public static ResponceLanguage CodeToEnum(string code)
        {
            return code switch
            {
                "ru" => ResponceLanguage.Russain,
                "en" => ResponceLanguage.English,
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string EnumToCode(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain => "ru",
                ResponceLanguage.English => "en",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string DatabaseUnavailable(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "В данный момент сервер с базой данных недоступен",
                ResponceLanguage.English =>
                "The server with the database is currently unavailable",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string UnidentifiedCommand(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Неопознанный синтаксис команды. Смотри список доступных команд в описании бота или используй команду /help",
                ResponceLanguage.English =>
                "Unidentified command syntax. See the list of available commands in the bot description or use the /help command",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string HelloMessage(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Привет, я информационный бот-помощник по игре Path of Exile. Могу выдавать разную полезную информацию или присылать новости. Используй комманду /help, чтобы увидеть список всех команд",
                ResponceLanguage.English =>
                "Hi, I'm the Path of Exile information bot. I can give out all sorts of useful information or send you news. Use the /help command to see a list of all commands",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string HelpMessage(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Список доступных команд:" +
                "\n/w название - Ссылка на wiki по запросу" +
                "\n/p предмет [6l или 5l] [| лига] - Цена предмета с графиком изменения. Опционально 5 или 6 линков и выбор лиги" +
                "\n/c имя героя полностью - Ссылка на профиль героя" +
                "\n/cl имя профиля полностью - Вывод всех персонажей профиля" +
                "\n/b предмет, камень умения или значимое пассивное умение - Вывод билдов на ниндзе с вещами из запроса." +
                " Можно указывать несколько вещей, разделенных знаком +" +
                "\n/i название - Скрин с wiki по запросу (работает медленно, проявите терпение:))" +
                "\n/l сложность или номер лабиринта - Вывод картинки с лайаутом выбранного лабиринта на сегодня" +
                "\n/h название лиги - Вывод картинки с подсказками к лиге" +
                "\n/sub en или /sub ru - Подписка беседы на новости на соответствующем языке" +
                "\n/top категория [количество] [группа] - Вывод топа предметов по цене из указанной категории. По умолчанию количество = 10, группы все" +
                "\n/hm название - Подсказка всех предметов по названию" +
                "\n/lang en или /lang ru - Изменить язык бота для этой беседы на указанный" +
                "\n\nЗапрос можно писать сокращено, если не указано обратное (например /p xo hea вместо /p Xoph's Heart)." +
                " Команды /p, /b, /l, /h, /top и /hm работают только с запросами на английском языке, все остальные также понимают русский",
                ResponceLanguage.English =>
                "List of available commands:" +
                "\n/w name - Wiki link on request" +
                "\n/p item [6l or 5l] [| league] - The price of an item with a timeline of changes. Optionally 5 or 6 links and league selection" +
                "\n/c full character's name - Character profile link" +
                "\n/cl full profile name - Display all characters of the profile" +
                "\n/b item, skill gem or keystone passive node - Output of poeninja builds with objects from the query." +
                " You can specify several objects separated by a +" +
                "\n/i name - Screenshot from wiki on request (works slowly, be patient:))" +
                "\n/l labyrinth difficulty or number - A picture with a layout of the selected labyrinth for today." +
                "\n/h league name - Picture with league hints" +
                "\n/sub en or /sub ru - Subscription of the chat to the news in the target language" +
                "\n/top category [quantity] [group] - Output the top items by price from the specified category. Default quantity = 10, groups all" +
                "\n/hm name - Hint of all items by name" +
                "\n/lang en or /lang ru - Change the bot language for this chat to the specified" +
                "\n\nA request can be written in shortened form, unless otherwise stated (like /p xo hea instead of /p Xoph's Heart).",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectLeagueKey(ResponceLanguage language, string leagues)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Некорректный ключ лиги. Список доступных лиг:\n{leagues}",
                ResponceLanguage.English =>
                $"Incorrect league key. List of available leagues:\n{leagues}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string UnableToObtainPrice(ResponceLanguage language, string request = null, string links = null)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Не удалось получить данные о ценах" +
                $"{(string.IsNullOrEmpty(request) ? "" : $" по запросу \"{request}\"{(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")}")}",
                ResponceLanguage.English =>
                "Price data failed to be received" +
                $"{(string.IsNullOrEmpty(request) ? "" : $" on request \"{request}\"{(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")}")}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string NoPriceData(ResponceLanguage language, string item, string league)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"О предмете {item} на лиге {league} нет данных о цене",
                ResponceLanguage.English =>
                $"There are no price data about {item} item in {league} league.",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string PriceResponce(ResponceLanguage language, string name, string links, string league, string tradeLink, JObject itemObject)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Цены на {name}{(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")} (лига {league})" +
                $"\nМинимальная: {Regex.Match((string)itemObject["min"], @"\d+[.]?\d{0,2}")}c" +
                $"\nСредняя: {Regex.Match((string)itemObject["median"], @"\d+[.]?\d{0,2}")}c" +
                $" ({Regex.Match((string)itemObject["exalted"], @"\d+[.]?\d{0,2}")}ex)" +
                $"\nСсылка на трейд: {tradeLink}",
                ResponceLanguage.English =>
                $"Prices for {name}{(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")} (league {league})" +
                $"\nMinimum: {Regex.Match((string)itemObject["min"], @"\d+[.]?\d{0,2}")}c" +
                $"\nAverage: {Regex.Match((string)itemObject["median"], @"\d+[.]?\d{0,2}")}c" +
                $" ({Regex.Match((string)itemObject["exalted"], @"\d+[.]?\d{0,2}")}ex)" +
                $"\nTrade Link: {tradeLink}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string ToManyResults(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Найдено слишком много возможных вариантов. Уточните запрос",
                ResponceLanguage.English =>
                "There are too many possible variants. Specify request",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string NoResults(ResponceLanguage language, string request)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"По запросу {request} ничего не найдено",
                ResponceLanguage.English =>
                $"Nothing was found on {request} request.",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string PossibleVariants(ResponceLanguage language, IEnumerable<string> variants)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Возможно вы искали:\n{string.Join("\n", variants)}",
                ResponceLanguage.English =>
                $"You may have been looking:\n{string.Join("\n", variants)}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string CouldntDetermine(ResponceLanguage language, string item)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Не удалось определить \"{item}\"",
                ResponceLanguage.English =>
                $"Couldn't identify \"{item}\"",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string SupportsNotSupported(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Камни поддержки не поддерживаются этой командой",
                ResponceLanguage.English =>
                "The support gems are not supported by this command",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string BuildsThatUse(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Билды которые используют",
                ResponceLanguage.English =>
                "Bilds that use",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string PlotTitle(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Цена в хаосах",
                ResponceLanguage.English =>
                "Price in chaos orbs",
                _ => string.Empty
            };
        }

        public static string SomethingWrong(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Что-то пошло не так",
                ResponceLanguage.English =>
                "Something goes wrong",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string ImageFailed(ResponceLanguage language, string name)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Не удалось вывести изображение статьи {name}",
                ResponceLanguage.English =>
                $"Failed to output the image of the article {name}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectLabDifficulty(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Неверно задана сложность лабиринта",
                ResponceLanguage.English =>
                "The difficulty of the labyrinth is incorrectly specified",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectHintKey(ResponceLanguage language, string request, IEnumerable<string> hints)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Не удалось вывести подсказку по запросу {request}\nПодсказки доступны по запросам: {string.Join(", ", hints)}",
                ResponceLanguage.English =>
                $"Couldn't get a hint on {request}\nHints are available in requests: {string.Join(", ", hints)}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string HintFormat(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Формат сообщения: category [quantity] [group]\nПо умолчанию quantity = 10, группы все",
                ResponceLanguage.English =>
                "Message format: category [quantity] [group]\nDefault quantity = 10, groups all",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectCategory(ResponceLanguage language, IEnumerable<string> categories)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Некорректная категория. Список доступных категорий:\n{string.Join("\n", categories)}",
                ResponceLanguage.English =>
                $"Incorrect category. The list of available categories:\n{string.Join("\n", categories)}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectGroup(ResponceLanguage language, JArray ja)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Неверно задана группа. Список доступных групп для данной категории:" +
                $"\n{string.Join("\n", ja.Children<JObject>().Select(o => o["group"].ToString()).Distinct())}",
                ResponceLanguage.English =>
                "The group is set incorrectly. The list of available groups for this category:" +
                $"\n{string.Join("\n", ja.Children<JObject>().Select(o => o["group"].ToString()).Distinct())}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string TopPricesResponce(ResponceLanguage language, int num, string category, string group, IEnumerable<JObject> results)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Топ {num} предметов по цене в категории {category}{(!string.IsNullOrEmpty(group) ? $" группы {group}" : "")}:" +
                $"\n{string.Join("\n", results.ToList().GetRange(0, num > results.Count() ? results.Count() : num).Select(o => $"{Regex.Match(o["median"].Value<string>(), @"\d+[.]?\d{0,2}")}c - {o["name"].Value<string>()}"))}",
                ResponceLanguage.English =>
                $"Top {num} items by price in category {category}, {(!string.IsNullOrEmpty(group) ? $" {group} group" : "")}:" +
                $"\n{string.Join("\n", results.ToList().GetRange(0, num > results.Count() ? results.Count() : num).Select(o => $"{Regex.Match(o["median"].Value<string>(), @"\d+[.]?\d{0,2}")}c - {o["name"].Value<string>()}"))}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string CharacterNotFound(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Указанный герой не найден",
                ResponceLanguage.English =>
                "The specified character was not found",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string ProfileNotFound(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Указанный профиль не найден",
                ResponceLanguage.English =>
                "The specified account was not found",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string CharListResponce(ResponceLanguage language, string account, JArray chars)
        {
            var charsDiscription = "";
            switch (language)
            {
                case ResponceLanguage.Russain:
                    {
                        foreach (var jt in chars)
                            charsDiscription += $"\n{jt["character"].Value<string>()} (лига: {jt["league"].Value<string>()}";
                        return $"Список доступных для отображения персонажей профиля {account}:\n{charsDiscription}";
                    }
                case ResponceLanguage.English:
                    {
                        foreach (var jt in chars)
                            charsDiscription += $"\n{jt["character"].Value<string>()} (league: {jt["league"].Value<string>()}";
                        return $"List of characters available to display in {account} profile:\n{charsDiscription}";
                    }
                default:
                    {
                        throw new Exception("Unrealized language");
                    }
            }
        }

        public static string SubscriptionFailed(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Ошибка подписки, попробуйте повторить позже",
                ResponceLanguage.English =>
                "Subscription error, try again later",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string RssSubscription(ResponceLanguage language, string subLanguage)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Эта беседа подписана на новости с {(subLanguage == "ru" ? "русского" : "английского")} сайта игры",
                ResponceLanguage.English =>
                $"This chat is subscribed to the news from the {(subLanguage == "ru" ? "Russian" : "English")} game site",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string RssUnsubscription(ResponceLanguage language, string subLanguage)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                $"Эта беседа отписана от новостей с {(subLanguage == "ru" ? "русского" : "английского")} сайта игры",
                ResponceLanguage.English =>
                $"This chat is unsubscribed from the news from the {(subLanguage == "ru" ? "Russian" : "English")} game site",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectLanguage(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Указан неверный язык",
                ResponceLanguage.English =>
                "Incorrect language specified",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string LanguageChanged(ResponceLanguage language)
        {
            return language switch
            {
                ResponceLanguage.Russain =>
                "Язык бота для этой беседы изменён на Русский",
                ResponceLanguage.English =>
                "The bot language for this chat has been changed to English",
                _ => throw new Exception("Unrealized language")
            };
        }
    }
}