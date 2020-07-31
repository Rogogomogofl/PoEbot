using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BotHandlers.Abstracts;

namespace BotHandlers
{
    public class ChatLanguage : AbstractChatLanguage
    {
        public override ResponseLanguage Language
        {
            get
            {
                language ??= langsDictionary.ContainsKey(id) ? langsDictionary[id] : ResponseLanguage.Russain;
                return (ResponseLanguage)language;
            }

            set
            {
                language = value;

                if (langsDictionary.ContainsKey(id))
                {
                    langsDictionary[id] = value;
                }
                else
                {
                    langsDictionary.Add(id, value);
                }

                try
                {
                    using var sw = new StreamWriter(langPath, false, Encoding.Default);
                    foreach (var lang in langsDictionary)
                        sw.WriteLine($"{lang.Key} {ResponseDictionary.EnumToCode(lang.Value)}");
                }
                catch (Exception ex)
                {
                    Logger.Log.Error($"{GetType()} {ex}");
                }
            }
        }

        public ChatLanguage(string langPath, long id, Dictionary<long, ResponseLanguage> langsDictionary) : base(langPath, id, langsDictionary) {}

        public static Dictionary<long, ResponseLanguage> LoadDictionary(string path)
        {
            Dictionary<long, ResponseLanguage> dictionary = new Dictionary<long, ResponseLanguage>();
            try
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parameters = line.Split(' ');
                    dictionary.Add(long.Parse(parameters[0]), ResponseDictionary.CodeToEnum(parameters[1]));
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{ex} at BotHandlers.ChatLanguage.LoadDictionary");
            }

            return dictionary;
        }
    }
}
