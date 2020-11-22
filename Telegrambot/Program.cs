using BotHandlers.Abstracts;
using TelegramBot.Workers;

namespace TelegramBot
{
    internal class Program
    {
        private static AbstractWorker _worker;

        private static void Main()
        {
            _worker = new TelegramBotWorker(@"bot/telegramcache.txt", @"bot/telegramsub.txt", @"bot/telegramlang.txt");
            _worker.Work();
        }
    }
}