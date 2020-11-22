using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotHandlers.Static
{
    public static class StaticLogger
    {
        private static StreamWriter _logger;
        private static readonly object Lock = new object();

        public static void LogError(Exception exception)
        {
            Task.Run(() =>
            {
                lock (Lock)
                {
                    _logger.WriteLine($"{DateTime.Now:yy-MM-dd HH:mm:ss} ERROR: {exception}{exception.StackTrace}\n");
                }
            });
        }

        public static void LogInfo(string info)
        {
            Task.Run(() =>
            {
                lock (Lock)
                {
                    _logger.WriteLine($"{DateTime.Now:yy-MM-dd HH:mm:ss} INFO: {info}\n");
                }
            });
        }

        public static void InitLogger(string subDir)
        {
            var dir = $@"{Directory.GetCurrentDirectory()}/bot/Logs/{subDir}";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var stream = new FileStream($@"{dir}/Log_{DateTime.Now:yy-MM-dd}.log", FileMode.Append, FileAccess.Write);
            _logger = new StreamWriter(stream, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }
    }
}
