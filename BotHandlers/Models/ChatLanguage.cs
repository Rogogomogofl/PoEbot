using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BotHandlers.Abstracts;
using BotHandlers.Methods;
using BotHandlers.Static;

namespace BotHandlers.Models
{
    public class ChatLanguage : AbstractChatLanguage 
    {
        public override ResponseLanguage Language
        {
            get => langsDictionary.TryGetValue(id, out var lang) ? lang : ResponseLanguage.Russain;
            set
            {
                langsDictionary[id] = value;

                try
                {
                    using var sw = new StreamWriter(langPath, false, Encoding.Default);
                    foreach (var lang in langsDictionary)
                    {
                        sw.WriteLine($"{lang.Key} {ResponseDictionary.EnumToCode(lang.Value)}");
                    }
                }
                catch (Exception ex)
                {
                    Common.Logger.LogError(ex);
                }
            }
        }

        public ChatLanguage(string langPath, long id, Dictionary<long, ResponseLanguage> langsDictionary) : base(langPath, id, langsDictionary) {}

        public static Dictionary<long, ResponseLanguage> LoadDictionary(string path)
        {
            var dictionary = new Dictionary<long, ResponseLanguage>();
            try
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parameters = line.Split(' ');
                    dictionary[long.Parse(parameters[0])] = ResponseDictionary.CodeToEnum(parameters[1]);
                }
            }
            catch (Exception ex)
            {
                Common.Logger.LogError(ex);
            }

            return dictionary;
        }
    }
}
