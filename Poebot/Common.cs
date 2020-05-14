using System;
using System.IO;
using System.Net;

namespace Bot
{
    static class Common
    {
        public static string GetContent(string url)
        {
            var output = "";
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(url);
                request.Proxy = null;
                using (var response = (HttpWebResponse) request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                    output = reader.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}: {e.Message} at Bot.Common.GetContent");
            }

            return output;
        }
    }
}