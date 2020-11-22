using System.Collections.Generic;
using BotHandlers.Methods;

namespace BotHandlers.Abstracts
{
    public abstract class AbstractChatLanguage
    {
        protected readonly string langPath;
        protected readonly long id;
        protected readonly Dictionary<long, ResponseLanguage> langsDictionary;

        public abstract ResponseLanguage Language { get; set; }

        protected AbstractChatLanguage(string langPath, long id, Dictionary<long, ResponseLanguage> langsDictionary)
        {
            this.langPath = langPath;
            this.id = id;
            this.langsDictionary = langsDictionary;
        }
    }
}
