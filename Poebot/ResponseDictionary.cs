using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    static class ResponseDictionary
    {
        public static string DatabaseUnavailable(string language)
        {
            switch (language)
            {
                case "ru":
                {
                    return "В данный момент сервер с базой данных недоступен";
                }
                case "en":
                {
                    return "";
                }
                default: return "";
            }
        }
    }
}