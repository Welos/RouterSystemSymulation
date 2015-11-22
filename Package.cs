using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router
{
    class Package  // klasa pakietu obslugiwanego przez router / 
    {              
        private static int numberOfPackages=0;
        private double size;      // wielkosc paczki w bajtach
        private double comingTime;// czas w którym połącznie pojawiło się w strumieniu 
        public Package()          // konstruktor domyslny, nie uzywany w symulacji
        {
            numberOfPackages++;
        }
        public Package( double m_size, double m_comingTime)
        {
            numberOfPackages++;
            size = m_size;
            comingTime = m_comingTime;
        }
        public static int GetNumberOfPackages() { return numberOfPackages; }
        public double GetSize() { return size; }
        public double GetComingTime() { return comingTime; }
    }

}
