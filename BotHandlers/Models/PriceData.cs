using System;
using System.Collections.Generic;

namespace BotHandlers.Models
{
    public class PriceData
    {
        public float PriceEx { get; }
        public float PriceChaosMin { get; }
        public float PriceChaosMedian { get; }
        public float PriceChaosMean { get; }
        public Dictionary<DateTime, float> PriceHistory { get; }
        public string TradeLink { get; }

        public PriceData(float priceChaosMin, float priceChaosMedian, float priceChaosMean, float priceEx, string tradeLink)
        {
            PriceChaosMin = priceChaosMin;
            PriceChaosMedian = priceChaosMedian;
            PriceChaosMean = priceChaosMean;
            PriceEx = priceEx;
            PriceHistory = new Dictionary<DateTime, float>();
            TradeLink = tradeLink;
        }
    }
}