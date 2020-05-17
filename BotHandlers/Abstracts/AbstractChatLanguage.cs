using System.Collections.Generic;

namespace BotHandlers.Abstracts
{
    public abstract class AbstractChatLanguage
    {
        protected readonly string langPath;
        protected readonly long id;
        protected readonly Dictionary<long, ResponceLanguage> langsDictionary;

        protected ResponceLanguage? language;
        public abstract ResponceLanguage Language { get; set; }

        protected AbstractChatLanguage(string langPath, long id, Dictionary<long, ResponceLanguage> langsDictionary)
        {
            this.langPath = langPath;
            this.id = id;
            this.langsDictionary = langsDictionary;
        }
    }
}
