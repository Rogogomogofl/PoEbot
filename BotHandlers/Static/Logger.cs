using System.IO;
using log4net;
using log4net.Config;

namespace BotHandlers.Static
{
    public static class Logger
    {
        public static ILog Log { get; } = LogManager.GetLogger("LOGGER");

        public static void InitLogger()
        {
            GlobalContext.Properties["LogFileName"] = $@"{Directory.GetCurrentDirectory()}/bot/";
            XmlConfigurator.Configure();
        }
    }
}
