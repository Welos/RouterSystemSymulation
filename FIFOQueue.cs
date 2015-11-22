using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router
{
    class FIFOQueue                                         //klasa ta symuluje działanie kolejki fifo
    {

        private Package[] queue;                            //tablica pakietów czekających na przetworzenie
        public double occupancy;                            //zajętość kolejki
        public int size;                                    //pojemność kolejki
        public int numberOfPackages;                        //ilość pakietów obecnych w kolejce
        private string name;                                //nazwa kolejki

        public int GetSize() { return size; }               //gettter wielkości kolejki (właściwie zbędny biorąc pod uwagę, że size jest publiczny)
        public double GetOccupancy() { return occupancy; }  //jw

        public FIFOQueue(string m_name, int m_size)         //konstruktor pustej kolejki o zadanej wielkości i nazwie
        {
            numberOfPackages = 0;
            name = m_name;
            occupancy = 0;
            size = m_size;
            queue = new Package[size*1000];

        }

        public void SetPackage(Package pack)                //metoda umieszczaająca na końcu kolejki nową paczkę 'pack'
        {
            if (this.occupancy+pack.GetSize()<this.size)
            {
                queue[numberOfPackages] = pack;
                numberOfPackages++;
                occupancy += pack.GetSize();
            }

        }

        public Package MoveToStream()                         //metoda zwracająca pierwszą paczkę i przesuwająca pozostałe paczki o 1 do przodu
        {
            Package tmp = queue[0];
            for (int j = 0; j < numberOfPackages - 1; j++)
                queue[j] = queue[j + 1];
            numberOfPackages--;
            occupancy -= tmp.GetSize();
            return tmp;
        }


    }
}
