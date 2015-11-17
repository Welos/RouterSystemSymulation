using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router
{
    class Package
    {
        private double size;
        private double comingTime;// czas w którym połącznie pojawiło się w strumieniu 
        public Package()
        {
        }
        public Package( double m_size, double m_comingTime)
        {
            size = m_size;
            comingTime = m_comingTime;
        }
        public double GetSize() { return size; }
        public double GetComingTime() { return comingTime; }
    }

}
