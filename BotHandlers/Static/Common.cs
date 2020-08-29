﻿using System;
using System.IO;
using System.Net;

namespace BotHandlers.Static
{
    public static class Common
    {
        public static string GetContent(string url)
        {
            var output = "";
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(url);
                request.Proxy = null;
                using var response = (HttpWebResponse) request.GetResponse();
                using var reader = new StreamReader(response.GetResponseStream());
                output = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{ex} at BotHandlers.Common.GetContent");
            }

            return output;
        }
    }
}