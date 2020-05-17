using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BotHandlers.Abstracts;

namespace BotHandlers
{
    public class ChatLanguage : AbstractChatLanguage
    {

        public override ResponceLanguage Language
        {
            get
            {
                language ??= langsDictionary.ContainsKey(id) ? langsDictionary[id] : ResponceLanguage.Russain;
                return (ResponceLanguage)language;
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
                catch (Exception e)
                {
                    Logger.Log.Error($"{e.Message} at {GetType()}");
                }
            }
        }

        public ChatLanguage(string langPath, long id, Dictionary<long, ResponceLanguage> langsDictionary) : base(langPath, id, langsDictionary) {}

        public static void LoadDictionary(string path, Dictionary<long, ResponceLanguage> dictionary)
        {
            try
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var parameters = line.Split(' ');
                    dictionary.Add(long.Parse(parameters[0]), ResponseDictionary.CodeToEnum(parameters[1]));
                }
            }
            catch (Exception e)
            {
                Logger.Log.Error($"{e.Message} at BotHandlers.ChatLanguage.LoadDictionary");
            }
        }
    }
}
