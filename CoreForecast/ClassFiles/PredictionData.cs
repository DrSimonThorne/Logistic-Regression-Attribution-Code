using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CoreForecast.ClassFiles.Extensions;

namespace CoreForecast.ClassFiles
{
    public class PredictionData : IDisposable
    {
        //base data
        public int visitorId { get; set; }
        public int totalEventstriggered { get; set; }
        public List<int> visitIds { get; set; }
        public List<int> listOfEvents { get; set; }
        public List<int> noneSaleEvents { get; set; }
        public Dictionary<int, double> trainingList { get; set; }
        public Dictionary<int, int> factorIDs { get; set; }
        public Dictionary<int, int> eventsTriggered { get; set; }
        public Dictionary<int, int> eventsTriggeredTotals { get; set; }

        public int on_site_duration { get; set; }
        public int views { get; set; }
        public int visits { get; set; }
        public DateTime visitStart { get; set; }
        public DateTime visitEnd { get; set; }
        public DateTime salesDate { get; set; }

        //transformed data
        public Dictionary<int, double> eventsTriggeredTotalsTransformed { get; set; }
        public double eventsCountTransformed { get; set; }
        public double durationTransformed { get; set; }
        public double viewsTransformed { get; set; }
        public double visitsTransformed { get; set; }

        //meta data
        public bool sale { get; set; }
        public double likelihood { get; set; }
        public bool containsOutier { get; set; }
        bool disposed = false;
        public PredictionData(Dictionary<int, int> factorIDs, List<int> noneSaleEvents, int visitorId, string visitIds, string eventString,
        int views, int visits, int duration, DateTime visitStart, DateTime visitEnd, bool sale, FrequencyData frequency)
        {
            this.visitorId = visitorId;
            this.views = views;
            this.visits = visits;
            this.on_site_duration = duration;
            this.visitStart = visitStart;
            this.visitEnd = visitEnd;
            this.sale = sale;
            this.factorIDs = factorIDs;
            this.noneSaleEvents = noneSaleEvents;
            eventsTriggeredTotals = new Dictionary<int, int>();

            foreach (var factor in factorIDs)
            {
                if (factor.Value > 0)
                {
                    eventsTriggeredTotals.Add(factor.Value, 0);
                }
            }

            eventStringSplitter(eventString);
            visitIdStringSplitter(visitIds);
            transformFactors(frequency);
        }
        private void eventStringSplitter(string eventString)
        {
            string[] splitEvents = Regex.Split(eventString, "\\D+");
            int eventCount = 0;
            listOfEvents = new List<int>();
            foreach (var evt in splitEvents)
            {
                if (evt.Equals(""))
                {
                    continue;
                }
                else
                {
                    if (factorIDs.Values.ToList().Contains(Convert.ToInt16(evt)))
                    {
                        listOfEvents.Add(Convert.ToInt16(evt));
                        eventCount++;
                    }
                }
                this.totalEventstriggered = eventCount;
            }
            foreach (var evt in listOfEvents)
            {
                try
                {
                    eventsTriggeredTotals[Convert.ToInt16(evt)]++;
                }
                catch (KeyNotFoundException knfe)
                {
                    Console.WriteLine($"event that caused the error: {evt}");
                    Console.WriteLine($"visitor_id = {this.visitorId}");
                    splitEvents.ToList().ForEach(Console.WriteLine);
                    Console.WriteLine("full list of eventsTriggeredTotals keys");
                    eventsTriggeredTotals.ToList().ForEach(s => Console.WriteLine(s.Key));
                    Console.WriteLine(knfe);
                    throw;
                }
            }
        }

        public void visitIdStringSplitter(string visitIds)
        {
            string[] splitVisits = Regex.Split(visitIds, "\\D+");
            this.visitIds = new List<int>();
            foreach (var visit in splitVisits)
            {
                if (visit.Equals(""))
                {
                    continue;
                }
                else
                {
                    this.visitIds.Add(Convert.ToInt32(visit));
                }
            }
        }
        public void transformFactors(FrequencyData frequency)
        {
            durationTransformed = ((float)on_site_duration - frequency.durationValuesMin) / (frequency.durationValuesMax - frequency.durationValuesMin);
            viewsTransformed = ((float)views - frequency.viewsValuesMin) / (frequency.viewsValuesMax - frequency.viewsValuesMin);
            visitsTransformed = ((float)visits - frequency.visitValuesMin) / (frequency.visitValuesMax - frequency.visitValuesMin);
            eventsCountTransformed = ((float)totalEventstriggered - frequency.eventsCountMin) / (frequency.eventsCountMax - frequency.eventsCountMin);
            eventsTriggeredTotalsTransformed = new Dictionary<int, double>();

            if (!durationTransformed.isFinite())
            {
                durationTransformed = 0;
            }
            if (!viewsTransformed.isFinite())
            {
                viewsTransformed = 0;
            }
            if (!visitsTransformed.isFinite())
            {
                visitsTransformed = 0;
            }
            if (!eventsCountTransformed.isFinite())
            {
                eventsCountTransformed = 0;
            }

            foreach (var evt in eventsTriggeredTotals.Keys)
            {
                if (eventsTriggeredTotals.Keys.Contains(evt))
                {
                    float temp = (
                            (float)eventsTriggeredTotals[evt] - frequency.eventValueMin[evt]) / (frequency.eventValueMax[evt] - frequency.eventValueMin[evt]);

                    if (temp.isFinite())
                    {
                        if (temp > 1.0)
                        {
                            //temp = 1;
                            containsOutier = true;
                        }
                        else if (temp < 0.0)
                        {
                            //temp = 0;
                            containsOutier = true;
                        }
                    }
                    else
                    {
                        temp = 0;
                    }
                    eventsTriggeredTotalsTransformed.Add(evt, temp);
                }
            }

            if (durationTransformed > 1)
            {
                durationTransformed = 1;
                //containsOutier = true;
            }
            if (viewsTransformed > 1)
            {
                viewsTransformed = 1;
                //containsOutier = true;
            }
            if (visitsTransformed > 1)
            {
                visitsTransformed = 1;
                //containsOutier = true;
            }
            if (eventsCountTransformed > 1)
            {
                eventsCountTransformed = 1;
                //containsOutier = true;
            }

        }
        public Dictionary<int, double> convertToDoubleTrain()
        {
            trainingList = new Dictionary<int, double>();
            trainingList[2] = durationTransformed;
            trainingList[3] = viewsTransformed;
            trainingList[4] = visitsTransformed;
            trainingList[5] = eventsCountTransformed;

            foreach (var factor in factorIDs)
            {
                if (factor.Value > 0)
                {
                    if (noneSaleEvents.Contains(factor.Value))
                    {
                        trainingList[factor.Key] = eventsTriggeredTotalsTransformed[factor.Value];
                    }
                }
            }

            trainingList[factorIDs.Keys.Max() + 1] = sale == true ? 1 : 0;

            return trainingList;
        }
        public Dictionary<int, double> convertToDoublePredict()
        {
            trainingList = new Dictionary<int, double>();
            trainingList[2] = durationTransformed;
            trainingList[3] = viewsTransformed;
            trainingList[4] = visitsTransformed;
            trainingList[5] = eventsCountTransformed;
            foreach (var factor in factorIDs)
            {
                if (factor.Value > 0)
                {
                    if (noneSaleEvents.Contains(factor.Value)) //.key?
                    {
                        trainingList[factor.Key] = eventsTriggeredTotalsTransformed[factor.Value];
                    }
                }
            }

            return trainingList;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;
        }
    }
}