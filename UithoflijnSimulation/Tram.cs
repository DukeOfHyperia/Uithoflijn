using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UithoflijnSimulation
{
    class Tram
    {
        public int id, passengers, prevTram;

        public Tram(int ID)
        {
            id = ID;
            passengers = 0;
            prevTram = -1;
        }
    }
}
