using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using BotHandlers.APIs;
using BotHandlers.Logger;
using BotHandlers.Methods;
using BotHandlers.Mocks;
using BotHandlers.Static;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BotUnitTests
{
    [TestClass]
    public class OtherApiTests
    {
        private readonly PoeApi api;
        private readonly Dictionary<long, ResponseLanguage> language = new Dictionary<long, ResponseLanguage>
        {
            { 0, ResponseLanguage.Russain }
        };

        public OtherApiTests()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Common.Logger = new ConsoleLogger();

            api = new PoeApi();
        }

        [TestMethod]
        public void LabLayoutTest()
        {
            var poebot = new Poebot(api, new MockPhoto(false), new MockLanguage(language));

            var message = poebot.ProcessRequest("/l merc");
            Assert.AreEqual(true, message?.Photo?.GetContent()?.Length == 1);

            message = poebot.ProcessRequest("/l 4");
            Assert.AreEqual(true, message?.Photo?.GetContent()?.Length == 1);
        }

        [TestMethod]
        public void RedditImageTest()
        {
            var poebot = new Poebot(api, new MockPhoto(false), new MockLanguage(language));

            var message = poebot.ProcessRequest("https://www.reddit.com/r/pathofexile/comments/i1nt6z/always_has_been/");
            Assert.AreEqual("Always has been", message?.Text);
            Assert.AreEqual(true, message?.Photo?.GetContent()?.Length == 1);

            message = poebot.ProcessRequest("https://www.reddit.com/r/pathofexile/comments/i1dhvh/til_the_artwork_of_a_dab_of_ink_has_a_person/");
            Assert.AreEqual("TIL The artwork of &quot;A Dab of Ink&quot; has a person literally dabbing...", message?.Text);
            Assert.AreEqual(true, message?.Photo?.GetContent()?.Length == 1);
        }

        [TestMethod]
        public void PobPartyTest()
        {
            var poebot = new Poebot(api, new MockPhoto(false), new MockLanguage(language));

            var message = poebot.ProcessRequest("https://pastebin.com/QZHfq4X9");
            var regex = new Regex(@"https:..pob.party.share.\w+");
            Assert.AreNotEqual(null, message);
            Assert.AreEqual(true, regex.IsMatch(message.Text));
        }
    }
}
