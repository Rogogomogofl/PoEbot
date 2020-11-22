using System;

namespace BotHandlers.Logger
{
    public interface ILogger
    {
        void LogError(Exception exception);
        void LogInfo(string info);
    }
}
