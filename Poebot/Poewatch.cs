using Newtonsoft.Json.Linq;
using System.Timers;

namespace Poebot
{
    public class Poewatch
    {
        private string[] tradeCategories = { "gem", "weapon", "accessory", "armour", "jewel", "flask", "card", "currency", "map", "prophecy" };
        private JArray itemsData = new JArray();
        private readonly object itemsDataLocker = new object();
        private Timer updateTimer = new Timer(3600 * 1000);

        public Poewatch()
        {
            updateTimer.Elapsed += onTimedEvent;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;
            loadItemdata();
        }

        public bool IsDataLoaded()
        {
            if (itemsData.Count > 0) return true;
            else return false;
        }

        private void onTimedEvent(object sender, ElapsedEventArgs e)
        {
            loadItemdata();
        }

        private void loadItemdata()
        { }
    }
}
