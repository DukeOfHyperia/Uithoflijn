using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UithoflijnSimulation
{
    public enum EventType { Arrival, Departure, ArrivalEnd, DepartureEnd };

    class Event
    {
        public int time, tram, station;
        public EventType type;

        public Event() { }
        public Event(int Time, int Tram, int Station, EventType Type)
        {
            time = Time;
            tram = Tram;
            station = Station;
            type = Type;
        }
    }
}
