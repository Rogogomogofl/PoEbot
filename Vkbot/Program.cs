using BotHandlers.Abstracts;
using VkBot.Workers;

namespace VkBot
{
    internal class Program
    {
        private static AbstractWorker _worker;

        private static void Main()
        {
            _worker = new VkBotWorker(@"bot/vkcache.txt", @"bot/vksub.txt", @"bot/vklang.txt");
            _worker.Work();
        }
    }
}