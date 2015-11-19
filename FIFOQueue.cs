using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router
{
    class FIFOQueue // queue FIFO. W tablicy przechowyjemy zgłoszenia, czyli próby połączenia oczekujące na zwolnienie się wiązki
    {

        private Package[] queue;
        public double occupancy;
        public int size;
        private int numberOfPackages;
        private string name;

        public int GetSize() { return size; }
        public double GetOccupancy() { return occupancy; }
        public Package[] GetQueue() { return queue; }

        public FIFOQueue(string m_name, int m_size)
        {
            numberOfPackages = 0;
            name = m_name;
            occupancy = 0;
            size = m_size;
            queue = new Package[size*1000];

        }

        public void SetPackage(Package pack)
        {
            numberOfPackages++;
            queue[numberOfPackages - 1] = pack;
            occupancy += pack.GetSize();
        }

        /*     public Package DeletePackage(int i)
             {
                 Package tmp = queue[i];
                 ChangeIndex(i);
                 numberOfPackages--;
                 occupancy -= tmp.GetSize();
                 return tmp;
             }*/

        public Package MoveToStream()
        {
            Package tmp = queue[0];
            ChangeIndex(0);
            numberOfPackages--;
            occupancy -= tmp.GetSize();
            return tmp;
        }

        public void ChangeIndex(int i)
        {
            for (int j = i; j < numberOfPackages - 1; j++)
                queue[j] = queue[j + 1];
        }
    }
}
