using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BotHandlers.Models;
using Newtonsoft.Json.Linq;

namespace BotHandlers
{
    public enum ResponseLanguage
    {
        Russain,
        English
    }

    public static class ResponseDictionary
    {
        public static ResponseLanguage CodeToEnum(string code)
        {
            return code switch
            {
                "ru" => ResponseLanguage.Russain,
                "en" => ResponseLanguage.English,
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string EnumToCode(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain => "ru",
                ResponseLanguage.English => "en",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string DatabaseUnavailable(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "В данный момент сервер с базой данных недоступен",
                ResponseLanguage.English =>
                "The server with the database is currently unavailable",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string UnidentifiedCommand(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Неопознанный синтаксис команды. Смотри список доступных команд в описании бота или используй команду /help",
                ResponseLanguage.English =>
                "Unidentified command syntax. See the list of available commands in the bot description or use the /help command",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string HelloMessage(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Привет, я информационный бот-помощник по игре Path of Exile." +
                " Могу выдавать разную полезную информацию или присылать новости." +
                " Используй комманду /help, чтобы увидеть список всех команд." +
                " To change the language of answers to English, use the command /lang en",
                ResponseLanguage.English =>
                "Hi, I'm the Path of Exile information bot." +
                " I can give out all sorts of useful information or send you news." +
                " Use the /help command to see a list of all commands." +
                " Чтобы изменить язык ответов на русский используйте команду /lang ru",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string HelpMessage(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
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
                //"\n/top категория [количество] [группа] - Вывод топа предметов по цене из указанной категории. По умолчанию количество = 10, группы все" +
                "\n/hm название - Подсказка всех предметов по названию" +
                "\n/lang en или /lang ru - Изменить язык бота для этой беседы на указанный" +
                "\n\nЗапрос можно писать сокращено, если не указано обратное (например /p xo hea вместо /p Xoph's Heart)." +
                " Команды /p, /b, /l, /h, /top и /hm работают только с запросами на английском языке, все остальные также понимают русский",
                ResponseLanguage.English =>
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
                //"\n/top category [quantity] [group] - Output the top items by price from the specified category. Default quantity = 10, groups all" +
                "\n/hm name - Hint of all items by name" +
                "\n/lang en or /lang ru - Change the bot language for this chat to the specified" +
                "\n\nA request can be written in shortened form, unless otherwise stated (like /p xo hea instead of /p Xoph's Heart).",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectLeagueKey(ResponseLanguage language, string leagues)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Некорректный ключ лиги. Список доступных лиг:\n{leagues}",
                ResponseLanguage.English =>
                $"Incorrect league key. List of available leagues:\n{leagues}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string UnableToObtainPrice(ResponseLanguage language, string request = null, string links = null)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Не удалось получить данные о ценах" +
                $"{(string.IsNullOrEmpty(request) ? "" : $" по запросу \"{request}\"{(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")}")}",
                ResponseLanguage.English =>
                "Price data failed to be received" +
                $"{(string.IsNullOrEmpty(request) ? "" : $" on request \"{request}\"{(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")}")}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string NoPriceData(ResponseLanguage language, string item, string league)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Ну удалось получить данные о ценах {item} на лиге {league}",
                ResponseLanguage.English =>
                $"Couldn't get price data about {item} in the {league} league",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string PriceResponce(ResponseLanguage language, string name, string links, string league, PriceData priceData)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Цены на {name}{(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")} (лига {league})" +
                $"\nМинимальная: {priceData.PriceChaosMin}c" +
                $"\nСредняя: {priceData.PriceChaosMedian}c" +
                $"\nУсреднённая: {priceData.PriceChaosMean}c" +
                $" ({priceData.PriceEx:n2}ex)" +
                $"\nСсылка на трейд: {priceData.TradeLink}",
                ResponseLanguage.English =>
                $"Prices for {name}{(!string.IsNullOrEmpty(links) ? " " + links + "L" : "")} (league {league})" +
                $"\nMin: {priceData.PriceChaosMin}c" +
                $"\nMedian: {priceData.PriceChaosMedian}c" +
                $"\nMean: {priceData.PriceChaosMean}c" +
                $" ({priceData.PriceEx:n2}ex)" +
                $"\nTrade Link: {priceData.TradeLink}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string ToManyResults(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Найдено слишком много возможных вариантов. Уточните запрос",
                ResponseLanguage.English =>
                "There are too many possible variants. Specify request",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string NoResults(ResponseLanguage language, string request)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"По запросу \"{request}\" ничего не найдено",
                ResponseLanguage.English =>
                $"Nothing was found on \"{request}\" request.",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string PossibleVariants(ResponseLanguage language, IEnumerable<string> variants)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Возможно вы искали:\n{string.Join("\n", variants)}",
                ResponseLanguage.English =>
                $"You may have been looking:\n{string.Join("\n", variants)}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string CouldntDetermine(ResponseLanguage language, string item)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Не удалось определить \"{item}\"",
                ResponseLanguage.English =>
                $"Couldn't identify \"{item}\"",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string SupportsNotSupported(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Камни поддержки не поддерживаются этой командой",
                ResponseLanguage.English =>
                "The support gems are not supported by this command",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string BuildsThatUse(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Билды которые используют",
                ResponseLanguage.English =>
                "Bilds that use",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string PlotTitle(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Цена в хаосах",
                ResponseLanguage.English =>
                "Price in chaos orbs",
                _ => string.Empty
            };
        }

        public static string SomethingWrong(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Что-то пошло не так",
                ResponseLanguage.English =>
                "Something goes wrong",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string ImageFailed(ResponseLanguage language, string name)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Не удалось вывести изображение статьи {name}",
                ResponseLanguage.English =>
                $"Failed to output the image of the article {name}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectLabDifficulty(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Неверно задана сложность лабиринта",
                ResponseLanguage.English =>
                "The difficulty of the labyrinth is incorrectly specified",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectHintKey(ResponseLanguage language, string request, IEnumerable<string> hints)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Не удалось вывести подсказку по запросу {request}\nПодсказки доступны по запросам: {string.Join(", ", hints)}",
                ResponseLanguage.English =>
                $"Couldn't get a hint on {request}\nHints are available in requests: {string.Join(", ", hints)}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string HintFormat(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Формат сообщения: category [quantity] [group]\nПо умолчанию quantity = 10, группы все",
                ResponseLanguage.English =>
                "Message format: category [quantity] [group]\nDefault quantity = 10, groups all",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectCategory(ResponseLanguage language, IEnumerable<string> categories)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Некорректная категория. Список доступных категорий:\n{string.Join("\n", categories)}",
                ResponseLanguage.English =>
                $"Incorrect category. The list of available categories:\n{string.Join("\n", categories)}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectGroup(ResponseLanguage language, JArray ja)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Неверно задана группа. Список доступных групп для данной категории:" +
                $"\n{string.Join("\n", ja.Children<JObject>().Select(o => o["group"].ToString()).Distinct())}",
                ResponseLanguage.English =>
                "The group is set incorrectly. The list of available groups for this category:" +
                $"\n{string.Join("\n", ja.Children<JObject>().Select(o => o["group"].ToString()).Distinct())}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string TopPricesResponce(ResponseLanguage language, int num, string category, string group, IEnumerable<JObject> results)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Топ {num} предметов по цене в категории {category}{(!string.IsNullOrEmpty(group) ? $" группы {group}" : "")}:" +
                $"\n{string.Join("\n", results.ToList().GetRange(0, num > results.Count() ? results.Count() : num).Select(o => $"{Regex.Match(o["median"].Value<string>(), @"\d+[.]?\d{0,2}")}c - {o["name"].Value<string>()}"))}",
                ResponseLanguage.English =>
                $"Top {num} items by price in category {category}, {(!string.IsNullOrEmpty(group) ? $" {group} group" : "")}:" +
                $"\n{string.Join("\n", results.ToList().GetRange(0, num > results.Count() ? results.Count() : num).Select(o => $"{Regex.Match(o["median"].Value<string>(), @"\d+[.]?\d{0,2}")}c - {o["name"].Value<string>()}"))}",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string CharacterNotFound(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Указанный герой не найден",
                ResponseLanguage.English =>
                "The specified character was not found",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string ProfileNotFound(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Указанный профиль не найден",
                ResponseLanguage.English =>
                "The specified account was not found",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string CharListResponce(ResponseLanguage language, string account, (string Name, string League)[] characters)
        {
            var charsDiscription = "";
            switch (language)
            {
                case ResponseLanguage.Russain:
                {
                    charsDiscription = characters.Aggregate(charsDiscription, (current, character) => current + $"\n{character.Name} (лига: {character.League}");
                    return $"Список доступных для отображения персонажей профиля {account}:\n{charsDiscription}";
                }
                case ResponseLanguage.English:
                {
                    charsDiscription = characters.Aggregate(charsDiscription, (current, character) => current + $"\n{character.Name} (league: {character.League}");
                    return $"List of characters available to display in {account} profile:\n{charsDiscription}";
                }
                default:
                    {
                        throw new Exception("Unrealized language");
                    }
            }
        }

        public static string SubscriptionFailed(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Ошибка подписки, попробуйте повторить позже",
                ResponseLanguage.English =>
                "Subscription error, try again later",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string RssSubscription(ResponseLanguage language, string subLanguage)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Эта беседа подписана на новости с {(subLanguage == "ru" ? "русского" : "английского")} сайта игры",
                ResponseLanguage.English =>
                $"This chat is subscribed to the news from the {(subLanguage == "ru" ? "Russian" : "English")} game site",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string RssUnsubscription(ResponseLanguage language, string subLanguage)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                $"Эта беседа отписана от новостей с {(subLanguage == "ru" ? "русского" : "английского")} сайта игры",
                ResponseLanguage.English =>
                $"This chat is unsubscribed from the news from the {(subLanguage == "ru" ? "Russian" : "English")} game site",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string IncorrectLanguage(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Указан неверный язык",
                ResponseLanguage.English =>
                "Incorrect language specified",
                _ => throw new Exception("Unrealized language")
            };
        }

        public static string LanguageChanged(ResponseLanguage language)
        {
            return language switch
            {
                ResponseLanguage.Russain =>
                "Язык бота для этой беседы изменён на Русский",
                ResponseLanguage.English =>
                "Bot language for this chat has been changed to English",
                _ => throw new Exception("Unrealized language")
            };
        }
    }
}