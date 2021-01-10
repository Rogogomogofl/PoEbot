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
    public class PoeApiTests
    {
        private readonly PoeApi _api;
        private readonly Dictionary<long, ResponseLanguage> _languages = new Dictionary<long, ResponseLanguage>
        {
            { 0, ResponseLanguage.Russain }
        };

        public PoeApiTests()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Common.Logger = new ConsoleLogger();

            _api = new PoeApi();
        }

        [TestMethod]
        public void WikiSearchTest()
        {
            var poebot = new Poebot(_api, new MockPhoto(false), new MockLanguage(_languages));

            var message = poebot.ProcessRequest("/w test");
            Assert.AreEqual(WebUtility.UrlDecode("https://pathofexile.gamepedia.com/Testudo"), WebUtility.UrlDecode(message?.Text));

            message = poebot.ProcessRequest("/w disf");
            Assert.AreEqual(WebUtility.UrlDecode("https://pathofexile.gamepedia.com/Atziri's_Disfavour"), WebUtility.UrlDecode(message?.Text));

            message = poebot.ProcessRequest("/w отвар рум");
            Assert.AreEqual(WebUtility.UrlDecode("https://pathofexile-ru.gamepedia.com/%D0%9E%D1%82%D0%B2%D0%B0%D1%80_%D0%A0%D1%83%D0%BC%D0%B8"), WebUtility.UrlDecode(message?.Text));
        }

        [TestMethod]
        public void PriceTest()
        {
            var poebot = new Poebot(_api, new MockPhoto(false), new MockLanguage(_languages));

            var message = poebot.ProcessRequest("/p exal orb");
            var regex = new Regex(@"Минимальная: \d+.*c");
            Assert.AreNotEqual(null, message);
            Assert.AreEqual(true, regex.IsMatch(message.Text));

            message = poebot.ProcessRequest("/p exal orb 6l");
            Assert.AreNotEqual(null, message);
            Assert.AreEqual(true, regex.IsMatch(message.Text));

            message = poebot.ProcessRequest("/p tabula ra");
            regex = new Regex(@"Минимальная: \d?[1-9]+\d?c");
            Assert.AreNotEqual(null, message);
            Assert.AreEqual(true, regex.IsMatch(message.Text));
        }

        [TestMethod]
        public void CharacterTest()
        {
            var poebot = new Poebot(_api, new MockPhoto(false), new MockLanguage(_languages));

            var message = poebot.ProcessRequest("/c Рогогомогофл");
            Assert.AreEqual(WebUtility.UrlDecode("http://poe-profile.info/profile/Rogogomogofl/Рогогомогофл"), WebUtility.UrlDecode(message?.Text));
        }

        [TestMethod]
        public void CharacterListTest()
        {
            var poebot = new Poebot(_api, new MockPhoto(false), new MockLanguage(_languages));

            var message = poebot.ProcessRequest("/cl rogogomogofl");
            var regex = new Regex(@"\S+\s[(]лига: .+");
            Assert.AreNotEqual(null, message);
            Assert.AreEqual(true, regex.Matches(message.Text).Count > 0);
        }

        [TestMethod]
        public void PoeNinjaBuildsTest()
        {
            var poebot = new Poebot(_api, new MockPhoto(false), new MockLanguage(_languages));

            var message = poebot.ProcessRequest("/b cyclon + starf");
            Assert.AreEqual("Билды которые используют Starforge + Cyclone:\nhttps://poe.ninja/challenge/builds?item=Starforge&skill=Cyclone", message?.Text);
        }

        [TestMethod]
        public void WikiScreenshotTest()
        {
            var poebot = new Poebot(_api, new MockPhoto(false), new MockLanguage(_languages));

            var message = poebot.ProcessRequest("/i test");
            Assert.AreEqual(true, message?.Photo?.GetContent()?.Length == 1);

            message = poebot.ProcessRequest("/i disfav");
            Assert.AreEqual(true, message?.Photo?.GetContent()?.Length == 1);
        }

        [TestMethod]
        public void HelpMeTest()
        {
            var poebot = new Poebot(_api, new MockPhoto(false), new MockLanguage(_languages));

            var message = poebot.ProcessRequest("/hm star");
            Assert.AreNotEqual(null, message);
            Assert.AreEqual(true, message.Text.Split(new[] { '\r', '\n' }).Length > 1);
        }
    }
}
