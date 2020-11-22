using System;

namespace BotHandlers.Logger
{
    public class ConsoleLogger : ILogger
    {
        public void LogError(Exception exception)
        {
            Console.WriteLine("---ERROR LOG---");
            Console.WriteLine($"{DateTime.Now:yy-MM-dd HH:mm:ss} ERROR: {exception}{exception.StackTrace}");
            Console.WriteLine("---END LOG---");
        }

        public void LogInfo(string info)
        {
            Console.WriteLine("---INFO LOG---");
            Console.WriteLine($"{DateTime.Now:yy-MM-dd HH:mm:ss} INFO: {info}");
            Console.WriteLine("---END LOG---");
        }
    }
}
