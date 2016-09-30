using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace UithoflijnSimulation
{
    class Program
    {
        static void Main()
        {
            Simulation sim = new Simulation(15);
            sim.Run();
        }
    }

    class Simulation
    {
        // definitions
        public int q = 3;           // turn around time
        public int r = 17;          // one-way driving time
        public int t;               // seconds between departures according to given frequency
        public int wagonSize = 210; // number of passengers per wagon

        // simulation variables
        public int time, previousTime;
        public Station[] track;
        public Tram[] trams;
        public Queue<Tram> storage;
        public SortedList<int, Event> events;
        public List<int> schedulePR, scheduleCS;
        public int pointerPR, pointerCS;

        // performance measurement
        public int nrDelays, totalWaitingTime, totalPassengers;

        public Simulation(int n)
        {
            // initialize simulation variables
            time = 0;
            previousTime = 0;
            this.buildTrack();
            this.buildVehicles((int)Math.Ceiling((double)n / 60 * (2 * (q + r))));
            events = new SortedList<int, Event>();

            // initialize definitions
            t = (60 / n) * 60;

            // initialize performance measurements
            nrDelays = 0;
            totalWaitingTime = 0;
            totalPassengers = 0;

            // create schedules for both endstations
            this.createSchedules(n);
            // schedule first departure at 06:00 from P+R
            events.Add(0, new Event(0, 0, 0, EventType.DepartureEnd));
        }

        public void Run()
        {
            Event now = new Event();

            // continue while there is still an event left
            while (events.Keys.Count != 0)
            {
                time = events.Keys.Min();
                // repeat if there are more events on this exact time
                while (events.Keys.Min() == time)
                {
                    now = events[events.Keys.Min()];
                    switch (now.type)
                    {
                        case EventType.Arrival:
                            this.handleArrival(now);
                            break;
                        case EventType.Departure:
                            this.handleDeparture(now);
                            break;
                        case EventType.ArrivalEnd:
                            this.handleArrivalEnd(now);
                            break;
                        case EventType.DepartureEnd:
                            this.handleDepartureEnd(now);
                            break;
                    }
                    events.Remove(events.Keys.Min());
                }
                previousTime = time;
            }
        }

        // MAIN FUNCTIONS
        private void handleArrival(Event now)
        {
            // check if station is empty and if tram hasn't overtaken the previous tram
            if (track[now.station].current != -1 || track[now.station].lastTram != trams[now.tram].prevTram)
            {
                // if so: add tram to queue of the station
                track[now.station].queue.Add(now.tram);
                return;
            }
            
            // mark station as occupied
            track[now.station].current = now.tram;

            // calculate the ammount of passengers unloading and loading
            int Pout = 0;
            trams[now.tram].passengers -= Pout;

            int loadLimitTime = (int)Math.Ceiling(12.5 + 0.22 * Pout);
            int Pin = 0;
            trams[now.tram].passengers += Pin;

            // check if number of passengers doesn't exceeds the trams limit
            if (trams[now.tram].passengers > 2 * wagonSize)
            {
                track[now.station].passengers = trams[now.tram].passengers - 2 * wagonSize;
                trams[now.tram].passengers = 2 * wagonSize;
                Pin -= track[now.station].passengers;
            }

            // calculate dwell time on the station
            int dwellTime = (int)Math.Ceiling(12.5 + 0.22 * Pin + 0.13 * Pout);
            if (track[now.station].lastDeparture - time + dwellTime < 40)
                dwellTime = 40;

            // TODO: add passenger waiting time to performance measurement
            totalPassengers += Pin;

            // schedule departure
            events.Add(time + dwellTime, new Event(time + dwellTime, now.tram, now.station, EventType.Departure));
        }
        private void handleDeparture(Event now)
        {
            // calculate travel time between stations
            int travelTime = 60; // TODO: travel time between stations + random

            // check if next station is an endstation
            EventType ArrivalType = EventType.Arrival;
            if (now.station == 7 || now.station == 15)
                ArrivalType = EventType.ArrivalEnd;

            // schedule arrival
            events.Add(time + travelTime, new Event(time + travelTime, now.tram, (now.station + 1) % 16, ArrivalType));

            // updating station information
            track[now.station].current = -1;
            track[now.station].lastTram = now.tram;
            track[now.station].lastDeparture = time;

            // if next tram waits in the queue, schedule the arrival of that tram on this station
            if (track[now.station].queue.Count != 0)
            {
                // find next tram
                int next = trams.Where(x => x.prevTram == now.tram).FirstOrDefault().id;    // this is an example of Linq, don't bother trying to understand the details
                if (track[now.station].queue.Contains(next))
                {
                    events.Add(time + 1, new Event(time + 1, next, now.station, EventType.Arrival));
                }
            }
        }
        private void handleArrivalEnd(Event now)
        {
            // check if station is empty and if tram hasn't overtaken the previous tram
            if ((track[now.station].current != -1 && track[now.station].current2 != -1) || track[now.station].lastTram != trams[now.tram].prevTram)
            {
                // if so: add tram to queue of the station
                track[now.station].queue.Add(now.tram);
                return;
            }
            
            // mark station as occupied
            if (track[now.station].current == -1)
                track[now.station].current = now.tram;
            else if (track[now.station].current2 == -1)
                track[now.station].current2 = now.tram;

            // all passengers leave the tram at the endstation
            trams[now.tram].passengers = 0;

            int Pin = 0;
            trams[now.tram].passengers += Pin;

            // check if number of passengers doesn't exceed the trams limit
            if (trams[now.tram].passengers > 2 * wagonSize)
            {
                trams[now.tram].passengers -= (track[now.station].passengers = trams[now.tram].passengers - 2 * wagonSize);
                Pin -= track[now.station].passengers;
            }

            // calculate dwell time on the endstation
            int turnAroundTime = r * 60; // turn around time in seconds

            // TODO: add passenger waiting time to performance measurement
            totalPassengers += Pin;

            int departTime;
            // check if tram 
            /*
            if (time + turnAroundTime > track[now.station].nextScheduledDep)
            {
                departTime = time + turnAroundTime;
                if (departTime >= track[now.station].nextScheduledDep + 60)
                    nrDelays++;
            }
            else
            {
                departTime = track[now.station].nextScheduledDep;
            }

            // schedule departure
            events.Add(departTime, new Event(departTime, now.tram, now.station, EventType.DepartureEnd));
            
            // schedule next departure
            if(time < this.toSeconds(07,00) || time >= this.toSeconds(19,00))
                track[now.station].nextScheduledDep += 15 * 60; // 4 per hour in morning and evening
            else
                track[now.station].nextScheduledDep += t;   // every t minutes otherwise
                */
        }
        private void handleDepartureEnd(Event now)
        {
            if (track[now.station].lastDeparture > time - 40)
            {
                events.Add(track[now.station].lastDeparture + 40, new Event(track[now.station].lastDeparture + 40, now.tram, now.station, EventType.DepartureEnd));
                return;
            }
            track[now.station].lastDeparture = time;
            track[now.station].lastTram = now.tram;
            track[now.station].current = -1;
            

            if (time > this.toSeconds(21, 30) && now.station == 0)
            {
                
            }
            else if (time < this.toSeconds(07, 00))
            {
                // ??
                int depTime = time + 15 * 60;
                int depTram = -1;
                events.Add(depTime, new Event(depTime, -1, now.station, EventType.DepartureEnd));   // TODO
            }
            else if (time < this.toSeconds(07, 2 * (r + q)))
            {

            }
            else if (time < this.toSeconds(19, 00) && now.station == 0)
            {

            }
            else if (time <= this.toSeconds(21, 30) && now.station == 0)
            {

            }
            else //if (time >)
            { }
            // schedule arrival
            // schedule departureEnd (first hour)
        }

        // SECONDARY FUNCTIONS
        private void buildTrack()
        {
            track = new Station[16];
            List<String> names = new List<String> { "WKZ", "UMC", "Heidelberglaan", "Padualaan", "Kromme Rijn", "Galgenwaard", "Vaartscherijn" };

            int i = 0;
            track[i] = new Station(0, "P+R De Uithof");
            for (i = 1; i <= names.Count; i++)
                track[i] = new Station(i, names[i - 1], true);
            track[i] = new Station(i, "Centraal Station Centrumzijde");
            for (int j = 1; i + j < 16; j++)
                track[i + j] = new Station(i + j, names[names.Count - j], false);

        }
        private void buildVehicles(int n)
        {
            trams = new Tram[n];
            storage = new Queue<Tram>();

            for (int i = 0; i < n; i++)
            {
                trams[i] = new Tram(i);
                storage.Enqueue(trams[i]);
            }

        }

        private void createSchedules(int n)
        {
            schedulePR = new List<int>();
            scheduleCS = new List<int>();

            pointerPR = 0;
            pointerCS = 0;

            int i;
            for(i = 0; i < this.toSeconds(07,00); i += 15*60)
            {
                schedulePR.Add(i);
                scheduleCS.Add(i + 60 * (q + r));
            }
            for (; i < this.toSeconds(19, 00); i += t)
            {
                schedulePR.Add(i);
                scheduleCS.Add(i + 60 * (q + r));
            }
            for (; i <= this.toSeconds(21,30); i += 15*60)
            {
                schedulePR.Add(i);
                scheduleCS.Add(i + 60 * (q + r));
            }
        }

        /*private int nextAvailableTram(int stationID, int tramID)
        {
            if (stationID == 0 && time < this.toSeconds(07, 2 * (r + q)))
            {
                if (track[0].current != -1)
                    return track[0].current;
                else
                    return storage.Dequeue().id;
            }
            else if (stationID == 0 && time > this.toSeconds(19,00))
            {
                if (track[0].current != -1)
                    return track[0].current;
            }
            else
            {
                return trams.Where(x => x.prevTram == tramID).FirstOrDefault().id;
            }
        }*/

        // TERTIARY FUNCTIONS
        private int toSeconds(int hour, int minutes, int seconds = 0)
        {
            return (hour - 6) * 3600 + minutes * 60 + seconds;
        }
    }
}
