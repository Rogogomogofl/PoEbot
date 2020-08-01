using System.Collections.Generic;
using BotHandlers.Abstracts;
using BotHandlers.Methods;

namespace BotHandlers.Mocks
{
    public class MockLanguage : AbstractChatLanguage
    {
        public override ResponseLanguage Language
        {
            get => langsDictionary[id]; 
            set => langsDictionary[id] = value;
        }
        public MockLanguage(Dictionary<long, ResponseLanguage> langsDictionary, string langPath = null, long id = 0) : base(langPath, id, langsDictionary) {}
    }
}
