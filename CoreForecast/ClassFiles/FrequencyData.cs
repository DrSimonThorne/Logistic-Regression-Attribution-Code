using System;
using System.Collections.Generic;
using CoreForecast.ClassFiles.Extensions;
namespace CoreForecast.ClassFiles
{
    public class FrequencyData
    {
        //duration data 
        public int durationValuesMin { get; set; }
        public int durationValuesMax { get; set; }

        //visit data 
        public int visitValuesMin { get; set; }
        public int visitValuesMax { get; set; }

        //events data
        public int eventsCountMin { get; set; }
        public int eventsCountMax { get; set; }
        public Dictionary<int, int> eventValueMin { get; set; }
        public Dictionary<int, int> eventValueMax { get; set; }

        //views data
        public int viewsValuesMin { get; set; }
        public int viewsValuesMax { get; set; }

        public FrequencyData(DbConnectionManager connection)
        {
            setFrequencyValues(connection);
        }

        public void setFrequencyValues(DbConnectionManager connection)
        {
            int tempMin, tempMax = 0;

            Console.WriteLine("Pulling: Factor Frequency");

            connection.getFrequencyData("on_site_duration", out tempMin, out tempMax); durationValuesMin = tempMin; durationValuesMax = tempMax;
            connection.getFrequencyData("events_count", out tempMin, out tempMax); eventsCountMin = tempMin; eventsCountMax = tempMax;
            connection.getFrequencyData("visits", out tempMin, out tempMax); visitValuesMin = tempMin; visitValuesMax = tempMax;
            connection.getFrequencyData("views", out tempMin, out tempMax); viewsValuesMin = tempMin; viewsValuesMax = tempMax;

            Dictionary<int, int> tempMins = new Dictionary<int, int>();
            Dictionary<int, int> tempMaxs = new Dictionary<int, int>();

            connection.getEventFrequencyData(out tempMins, out tempMaxs);

            eventValueMin = tempMins;
            eventValueMax = tempMaxs;

        }
    }
}