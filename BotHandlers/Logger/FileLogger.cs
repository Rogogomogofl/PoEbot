using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotHandlers.Logger
{
    public class FileLogger : ILogger
    {
        private StreamWriter _logger;
        private readonly object Lock = new object();

        public FileLogger(string subDir)
        {
            var dir = $@"{Directory.GetCurrentDirectory()}/bot/Logs/{subDir}";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var stream = new FileStream($@"{dir}/Log_{DateTime.Now:yy-MM-dd}.log", FileMode.Append, FileAccess.Write);
            _logger = new StreamWriter(stream, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        public void LogError(Exception exception)
        {
            Task.Run(() =>
            {
                lock (Lock)
                {
                    _logger.WriteLine($"{DateTime.Now:yy-MM-dd HH:mm:ss} ERROR: {exception}{exception.StackTrace}\n");
                }
            });
        }

        public void LogInfo(string info)
        {
            Task.Run(() =>
            {
                lock (Lock)
                {
                    _logger.WriteLine($"{DateTime.Now:yy-MM-dd HH:mm:ss} INFO: {info}\n");
                }
            });
        }
    }
}
