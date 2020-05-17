using System.Collections.Generic;
using BotHandlers;
using BotHandlers.Abstracts;

namespace TestBot.Mocks
{
    internal class MockLanguage : AbstractChatLanguage
    {
        public override ResponceLanguage Language
        {
            get => langsDictionary[id]; 
            set => langsDictionary[id] = value;
        }
        public MockLanguage(Dictionary<long, ResponceLanguage> langsDictionary, string langPath = null, long id = 0) : base(langPath, id, langsDictionary) {}
    }
}
