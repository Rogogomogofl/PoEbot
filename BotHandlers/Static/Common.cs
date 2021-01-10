using System;
using System.IO;
using System.Net;
using BotHandlers.Logger;

namespace BotHandlers.Static
{
    public static class Common
    {
        public static string GetContent(string url)
        {
            var output = "";
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null;
                request.UserAgent = "poe-bot";
                using var response = (HttpWebResponse)request.GetResponse();
                using var reader = new StreamReader(response.GetResponseStream());
                output = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex);
            }

            return output;
        }

        public static ILogger Logger { get; set; }
    }
}