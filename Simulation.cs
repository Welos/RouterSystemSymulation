using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

namespace Router
{
    class Simulation
    {

        ////ZMIENNE

        double[] NumberOfReceivedPackages;        //liczba pakietów, które się pojawiły w routerze
        double[] NumberOfServedPackages;          //liczba pakietów obsłużonych przez scheduler (nazywany później przez nas routerem)
        double[] ProbabilityOfFailure;            //prawdopodobieństwo odrzucenia pakietu przez router
        double[] rejected;                        //liczba odrzuconych pakietów 
        string[] TableOfPriorities;               //tablica służąca przyporządkowaniu priorytetó nazwom kolejek
        int NumberOfDistributions;                //liczba rozkładów, którymi posługujemy się w symulacji
        int NumberOfQueues;                       //liczba kolejek w routerze
        int NumberOfStreams;                      //liczba strumieni wejściowych
        double[] AverageServingTime;              //średni czas obsługi pakietu (liczony tylko dla pakietów przyjętych do schedulera)
        double[] TotalServingTime;                //tablica pomocnicza służąca do wyznaczania powyższej wartości
        double[] TotalOccupancyOfQueue;           //tablica pomocnicza służąca do wyznaczania poniższej wartości
        double[] AverageOccupancyOfqueue;         //średnia zajętość poszczególnych kolejek
        double TimeOfSimulation;                  //całkowity czas symulacji podawany przez użytkownika (w ms )
        double CurrentTime;                       //czas od rozpoczęcia symulacji
        double AuxilaryTime;                      //odcinek czasu od poprzedniego do obecnego wydarzenia, służy m in do liczenia zajętości kolejek
        ProbabilityDistributions[] distribution;  //tablica rozkładów z których korzystamy
        FIFOQueue[] queue;                        //tablica kolejek FIFO 
        Heap  eventQueue;                         //stóg przehwoujący zdarzenia posegregowane wg klucza
        Stream[] stream;                          //tablica strumieni wejściowych
        bool occupancyOfRouter;                   //zmienna boolowska mówiąca o tym, czy scheduler jest zajęty (true==zajęty)
        string inputDirectory;                    //ścieżka pliku wejściowego
        string outputDirectory;                   //ścieżka pliku wyjściowego
        string systemName;                        //nazwa systemu, który symulujemy
        int flowability;                          //przepływność schedulera (przeskalowana na ms)
        Package ProcessedPackage;                 //paczka znajdująca się w danym momencie w schedulerze
        double TotalOccupancyOfRouter;            //zmienna pomocnicza służąca do wyznaczania poniższej wartości
        double AverageOccupancyOfRouter;          //średnia zajętość schedulera

        public Simulation ()   //konstruktor domyślny
        {
            TotalOccupancyOfRouter = 0;
            Package ProcessedPackage = new Package();
            NumberOfStreams = 0;
            GetData();     //Uruchamiamy metodę wczytującą dane wejściowe z pliku i ustalającą ścieżkę wyjściową 
            TimeOfSimulation = CurrentTime =AuxilaryTime= 0;
            occupancyOfRouter = false;
            eventQueue=new Heap();
            NumberOfReceivedPackages = new double[NumberOfStreams + 1];   //indeksy od 0 do NumberOfStreams -1 odnoszą 
            NumberOfServedPackages = new double[NumberOfStreams + 1];     //się do statystyk cząstkowych dla kolejnych buforów(kolejek)
            ProbabilityOfFailure = new double[NumberOfStreams + 1];       //indeks Number of streams zbiera sumę ze wszystkich kolejek
            AverageServingTime = new double[NumberOfStreams + 1];
            TotalServingTime = new double[NumberOfStreams + 1];
            AverageOccupancyOfqueue = new double[NumberOfStreams];        //sensowność sumowania zajętości kolejek jest znikoma
            TotalOccupancyOfQueue = new double[NumberOfStreams];          //więc tego nie robimy i nie dodajemy dodatkowego indeksu
            rejected = new double[NumberOfStreams +1];
            TotalOccupancyOfRouter = 0;
            AverageOccupancyOfRouter = 0;
            for (int i=0;i<=NumberOfStreams;i++)                          //w tej i następnej pętli ustalamy zerowe wartości zmiennych
            {                                                             //służacych statystyce
                rejected[i] = 0;
                TotalServingTime[i] = 0;
                AverageServingTime[i] = 0;
                NumberOfReceivedPackages[i] = 0;
                NumberOfServedPackages[i] = 0;
                ProbabilityOfFailure[i] = 0;
            }
            for (int i = 0; i < NumberOfStreams; i++)
            {
                AverageOccupancyOfqueue[i] = 0;
                TotalOccupancyOfQueue[i] = 0;
            }



        }
        public void fSimulation() //Ciało i dusza naszej symulacji
        {
            double previoustime=0;
            double CurrentTime = 0;
            Random rnd= new Random(DateTime.Now.Millisecond);
            while (TimeOfSimulation==0)  
            {
                try
                {
                    Console.WriteLine("Prosze podac dodatni czas symulacji w ms: ");   //czas pobieramy w ms aby uniknąć działań na liczbach z dużą liczbą cyfr po przecinku
                    TimeOfSimulation = double.Parse(Console.ReadLine());               //generowanych np poprzez dzielenie małych liczb przez duże liczby
                    if (TimeOfSimulation <= 0) throw new Exception();                  //podczas obliczania czasu pobytu paczki w schedulerze, oraz  

                }
                catch (Exception)
                {
                    Console.WriteLine("Prosze podaj inna liczbe ");
                }
                for (int i=0;i<NumberOfStreams;i++)                                    //dla każdego strumienia generujemy pierwsze zdarzenie pojawienia się pakietu,
                {                                                                      //a następnie umieszczamy je na stogu
                    string n=stream[i].GetNameOfWaitingDistribution();
                    double wait=Extractvalue(n,NumberOfStreams);
                    eventQueue.Insert(Event.Coming(i, wait)); 
                }

            }
            while (CurrentTime<TimeOfSimulation)                     //każda iteracja tej pętli symuluje obsługę jednego zdarzenia
            {
                Event TempEvent=eventQueue.Delete();                 //pobieramy zdarzenie o najniższej wartości klucza
                previoustime = CurrentTime;
                CurrentTime=TempEvent.getKey();                      //przesuwamy czas do obecnego zdarzenia
                AuxilaryTime = CurrentTime - previoustime;
                for (int i=0;i<NumberOfStreams;i++)                  //liczymy zajętość kolejek od poprzedniego do obecnego zdarzenia
                {
                    TotalOccupancyOfQueue[i] +=( queue[i].GetOccupancy()/queue[i].GetSize()) * AuxilaryTime;
                }
                if (occupancyOfRouter == true)                       //liczymy zajętość schedulera
                    TotalOccupancyOfRouter += AuxilaryTime;

                
                  switch(TempEvent.GetKind())                        //sprawdzamy jakiego rodzaju zdarzenie obsługujemy
                  {
                      case KindOfEvent.Coming:                       //pojawienie się pakietu na wejściu
                          {
                              double length=8*Extractvalue(stream[TempEvent.getStreamNumber()].GetNameOflengthDistribution(),NumberOfStreams);  //losujemy rozmiar paczki za pomocą dystrybuanty przypisanej do strumienia na którym pojawiła się paczka
                              Package TemporaryPackage = new Package(length,TempEvent.getKey());                                              //tworzymy paczkę o zadanym rozmiarze i czasie pojawienia się
                              NumberOfReceivedPackages[TempEvent.getStreamNumber()]++;
                              NumberOfReceivedPackages[NumberOfStreams ]++;

                              if (occupancyOfRouter==false)                //jeśli scheduler jest wolny w czasie przyjściaa paczki to w buforach nie czeka też żadna inna paczka
                              {                                            //więc możemy "wrzucić" przychodzącą paczkę bezpośrednio do schedulera
                                  occupancyOfRouter = true;                //oraz stworzyć wydarzenie opisujące jej opuszczenie systemu
                                  double processing = length/flowability;
                                  eventQueue.Insert(Event.GoingOutOfRouter(TempEvent.getStreamNumber(), CurrentTime+processing));
                                  ProcessedPackage = TemporaryPackage;
                     
                              }      
                                                                           //w poniższym warunku sprawdzamy czy paczka zmieści się do kolejki, jeśli tak to umieszczamy ją na końcu (zgodnie z zasadą FIFo)
                              else if (queue[stream[TempEvent.getStreamNumber()].bufor].occupancy + TemporaryPackage.GetSize() < queue[stream[TempEvent.getStreamNumber()].bufor].size)
                              {
                                  queue[stream[TempEvent.getStreamNumber()].bufor].SetPackage(TemporaryPackage);
                              }
                              else                                         //jeśli paczka nie mieści się do kolejki, to jest ona odrzucana
                              {
                                  rejected[TempEvent.getStreamNumber()]++;
                                  rejected[NumberOfStreams]++;
                              }                                   
                                                                           //po obsłużeniu strumienia wejściowego należy zapalnować następne wydarzenie na tym strumieniu
                              double interval = Extractvalue(stream[TempEvent.getStreamNumber()].GetNameOfWaitingDistribution(),NumberOfStreams);
                              eventQueue.Insert(Event.Coming(TempEvent.getStreamNumber(), CurrentTime + interval));
                                    
                          break;
                          }
                      case KindOfEvent.GoingOutOfRouter:                   //obsługa pakietu w schedulerze
                          {
                                                                           //poniżej zbieramy dane o obsłużonej paczce
                              double ServingTime = CurrentTime - ProcessedPackage.GetComingTime();
                              TotalServingTime[TempEvent.getStreamNumber()] += ServingTime;
                              TotalServingTime[NumberOfStreams] += ServingTime;
                              NumberOfServedPackages[TempEvent.getStreamNumber()]++;
                              NumberOfServedPackages[NumberOfStreams] = NumberOfServedPackages[NumberOfStreams] + 1;
                              occupancyOfRouter = false;
                                  for (int i = 0; i < NumberOfQueues; i++) //w tej pętli sprawdzamy czy w kolejkach (od najwyższego priorytetu do najniższego)
                                  {                                        //znajdują się jakieś oczekujące na obsługę paczki
                                      if (queue[i].numberOfPackages > 0)   //pierwszą znalezioną paczkę "przenosimy" do schedulera i planujemy wydarzenie końca jej obsługi
                                      {
                                          occupancyOfRouter = true;
                                          ProcessedPackage = queue[i].MoveToStream();  
                                          double processing =ProcessedPackage.GetSize()/ flowability;
                                          eventQueue.Insert(Event.GoingOutOfRouter(i, CurrentTime + processing));
                                          break;
                                      }
                                  }
                              break;
                           }

                  }
                  
            }
            SumUp();                                       //opracowujemu wyniki
            WriteResults();                                //wypisujemy wyniki do wcześniej określonego pliku txt
            Console.WriteLine("Zakonczono symulacje");

        }

        public void GetData()                              //metoda ta służy do stworzenia obiektó takich jak kolejki, rokłady i strumienie, o wartościach zadanych w pliku wejściowym
        {

            bool ok = false;
            while (!ok)
            {
                StreamReader sr;
                string[] strings;
                ok = true;
                try
                {
                    Console.WriteLine("Przeciagnij tu plik wejsciowy i wcisnij ENTER...");
                    inputDirectory = Console.ReadLine();
                    if (inputDirectory[0] == '\"') inputDirectory = inputDirectory.Substring(1, inputDirectory.Length - 2);
                    Console.WriteLine(" ");
                    sr = new StreamReader(inputDirectory);
                    String line = "";
                    # region nazwa systemu         
                    while (line.Length < 2 || line[0] == '#')      //upewniamy się, że załadowaliśmy plik wejściowy przeznaczony do naszej symulacji
                    {
                        line = sr.ReadLine();
                    }
                    strings = line.Split(' ');
                    if (strings[0] == "SYSTEM" && strings[2] != "") systemName = strings[2];
                    else throw (new Exception("Zla nazwa systemu"));
                    #endregion
                    #region flow
                    line = "";
                    while (line.Length < 2 || line[0] == '#')              //pobieramy wartość przepływności schedulera
                    {
                        line = sr.ReadLine();
                    }
                    strings = line.Split(' ');
                    if (strings[0] == "PRZEPLYWNOSC" && strings[2] != "")
                    {
                        flowability = int.Parse(strings[2]);
                        flowability = flowability / 1000;                   //dla czasu liczonego w ms przepływność jest 1000 razy mniejsza
                    }
                    else throw (new Exception("Zla przeplywnosc"));
                    #endregion
                    #region numberOfBufors
                    line = "";
                    while (line.Length < 2 || line[0] == '#')     //pobieramy liczbę kolejek FIFO
                    {
                        line = sr.ReadLine();
                    }
                    strings = line.Split(' ');
                    if (strings[0] == "KOLEJKI" && strings[2] != "")
                    {
                        NumberOfQueues = int.Parse(strings[2]);
                    }
                    else throw (new Exception("Zla liczba pojemności kolejki"));
                    #endregion
                    #region readBufors
                    queue = new FIFOQueue[NumberOfQueues];                 //Tworzymy kolejki o zadanych wielkościach i nazwach
                    TableOfPriorities = new string[NumberOfQueues];
                    for (int i = 0; i < NumberOfQueues; i++)
                    {
                        string name;
                        int size;
                        line = "";
                        while (line.Length < 2 || line[0] == '#')
                        {
                            line = sr.ReadLine();
                        }
                        strings = line.Split(' ');
                        if (strings[0] == "NAZWA" && strings[2] != "" && strings[3] == "BUFOR" && strings[5] != "")
                        {
                            name = strings[2];
                            size = int.Parse(strings[5]);
                            size *= 8;                                    //zamieniamy rozmiar bufora na bity    
                        }
                        else throw (new Exception("Zle kodowanie bufora"));
                        line = "";

                        TableOfPriorities[i] = name;
                        queue[i] = new FIFOQueue(name, size);
                    }
                    #endregion
                    #region liczba rozkladow
                    line = "";
                    while (line.Length < 2 || line[0] == '#')       
                    {
                        line = sr.ReadLine();
                    }
                    strings = line.Split(' ');
                    if (strings[0] == "ROZKLADY" && strings[2] != "")
                        NumberOfDistributions = int.Parse(strings[2]);
                    else throw (new Exception("Zla liczba rozkładów"));
                    #endregion
                    #region reading distributions                              
                    distribution = new ProbabilityDistributions[NumberOfDistributions];
                    for (int i = 0; i < NumberOfDistributions; i++)                //tworzymy rozkłady o podanych nazwach i wartościach oczekiwanych
                    {
                        string name;
                        double lambda;
                        line = "";
                        while (line.Length < 2 || line[0] == '#')
                        {
                            line = sr.ReadLine();
                        }
                        strings = line.Split(' ');
                        if (strings[0] == "NAZWA" && strings[2] != "")
                        {
                            name = strings[2];
                        }
                        else throw (new Exception("Zla nazwa rozkladu"));
                        line = "";
                        while (line.Length < 2 || line[0] == '#')
                        {
                            line = sr.ReadLine();
                        }
                        strings = line.Split(' ');
                        if (strings[0] == "LAMBDA" && strings[2] != "")
                        {
                            lambda = double.Parse(strings[2]);

                        }
                        else throw (new Exception("Zla lambda rozkladu"));

                        distribution[i] = new ProbabilityDistributions(name, lambda);
                    }
                    #endregion
                    #region liczba strumieni
                    line = "";
                    while (line.Length < 2 || line[0] == '#')
                    {
                        line = sr.ReadLine();
                    }
                    strings = line.Split(' ');
                    if (strings[0] == "STRUMIENIE" && strings[2] != "")
                        NumberOfStreams = int.Parse(strings[2]);
                    else throw (new Exception("Zla liczba strumieni"));
                    #endregion
                    #region wczytywanie strumieni
                    stream = new Stream[NumberOfStreams];
                    for (int i = 0; i < NumberOfStreams; i++)
                    {
                        string name;
                        string buforInString;
                        int bufor = 0;
                        string waitingTime;
                        string size;
                        line = "";
                        while (line.Length < 2 || line[0] == '#')
                        {
                            line = sr.ReadLine();
                        }
                        strings = line.Split(' ');
                        if (strings[0] == "NAZWA" && strings[2] != "" && strings[3] == "KOLEJKA" && strings[5] != "" &&
                            strings[6] == "CZAS" && strings[8] != "" && strings[9] == "WIELKOSC" &&
                            strings[11] != "")
                        {
                            name = strings[2];
                            buforInString = strings[5];
                            waitingTime = strings[8];
                            size = strings[11];
                        }
                        else throw (new Exception("Zle dane strumienia"));
                        for (int j = 0; j < TableOfPriorities.Length;j++ )  //za pomocą nazwy bufora znajdujemy jego indeks (a co za tym idzie priorytet)
                        {
                            if (buforInString == TableOfPriorities[j])
                            {
                                bufor = j;
                                break;
                            }

                        }
                            stream[i] = new Stream(name, size, waitingTime, bufor, bufor);
                    }
                    #endregion

                    Console.WriteLine("Przeciagnij tu plik wyjsciowy i wcisnij ENTER...");   //ustalamy ścieżkę wyjściową dla danych
                    outputDirectory = Console.ReadLine();
                    Console.WriteLine(" ");
                    if (outputDirectory[0] == '"') outputDirectory = outputDirectory.Substring(1, outputDirectory.Length - 2);
                }
                catch (Exception def)
                {
                    Console.WriteLine("Zla sciezka. Sprobuj jeszcze raz.");
                    Console.WriteLine(def.Message);
                    ok = false;
                }

            }
        }

        public void SumUp()                                         //w tej funkcji dokonujemy ostatnich przekształceń na danych statystycznych
        {
            for (int i = 0; i < NumberOfStreams; i++)
            {
                double toq=TotalOccupancyOfQueue[i];
                double tst = TotalServingTime[i];
                double nsp=NumberOfServedPackages[i];
                double r=rejected[i];
                double nrp=NumberOfReceivedPackages[i];
                AverageOccupancyOfqueue[i] = (toq / TimeOfSimulation)*100;
                AverageServingTime[i] =  tst/ nsp;
                ProbabilityOfFailure[i] = (r / nrp)*100;
            }
            double tst1 = TotalServingTime[NumberOfStreams];
            double nsp1 = NumberOfServedPackages[NumberOfStreams];
            double r1 = rejected[NumberOfStreams];
            double nrp1 = NumberOfReceivedPackages[NumberOfStreams];
            AverageServingTime[NumberOfStreams ] = tst1 / nsp1;
            ProbabilityOfFailure[NumberOfStreams] = (r1 / nrp1)*100;
            AverageOccupancyOfRouter = (TotalOccupancyOfRouter / TimeOfSimulation)*100;

        }
        public void WriteResults()                                  //funkcja ta wypisuje wyniki do pliku
        {
            StreamWriter routerSimulationResults = new StreamWriter(outputDirectory);

            routerSimulationResults.WriteLine("System = " + systemName);
            for (int i=0; i<NumberOfStreams;i++)
            {
                routerSimulationResults.WriteLine("Srednia zajetosc bufora {0} : " + Math.Round(AverageOccupancyOfqueue[i],2)+"%", TableOfPriorities[i]);
                routerSimulationResults.WriteLine("Sredni czas obslugi pakietu o priorytecie {0} : " + Math.Round(AverageServingTime[i],2), TableOfPriorities[i]);
                routerSimulationResults.WriteLine("Prawdopodobienstwo odrzucenia ze strumienia {0} : " +Math.Round( ProbabilityOfFailure[i],2)+"%", i);
            }

            routerSimulationResults.WriteLine();
            routerSimulationResults.WriteLine("Sredni czas obsugi pakietu : " + Math.Round(AverageServingTime[NumberOfStreams],2));
            routerSimulationResults.WriteLine("Prawdopodobienstwo odrzucenia z Routera : " + Math.Round(ProbabilityOfFailure[NumberOfStreams ],2)+"%");
            routerSimulationResults.WriteLine("Srednia zajetosc Routera : " + Math.Round(AverageOccupancyOfRouter,2)+"%");

            routerSimulationResults.Close();
        }
        public double Extractvalue (string name, int number)        //funkcja ta pobiera nazwę rozkładu i zwraca losową wartość otrzymaną z tego rozkładu
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            double value;
            int j=0;
            for (int i=0;i<number;i++)
            {
                j=i;
                if (distribution[i].GetName() == name)
                    break;

            }
            value = distribution[j].SetTime(rnd.NextDouble());
            value=Math.Round(value, 3);
            if (value <= 0.001)
                value = 0.001;
            return value;
        }
    }
}
