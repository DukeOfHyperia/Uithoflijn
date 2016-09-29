using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UithoflijnSimulation
{
    class Station
    {
        // general variables
        public String name;
        public int id, lastDeparture, lastLoad, lastTram, current, passengers;
        public List<int> queue;

        // station variable
        public Boolean toStation;

        // endstation variable
        public int current2, nextScheduledDep;

        public Station() { }
        public Station(int ID, String Name)
        {
            id = ID;
            name = Name;
            lastDeparture = 0;
            queue = new List<int>();
            current = -1;
            current2 = -1;
            nextScheduledDep = 0;
            passengers = 0;
        }
        public Station(int ID, String Name, Boolean ToStation)
        {
            id = ID;
            name = Name;
            lastDeparture = 0;
            lastLoad = 0;
            lastTram = -1;
            toStation = ToStation;
            queue = new List<int>();
            current = -1;
            passengers = 0;
        }
    }
}
