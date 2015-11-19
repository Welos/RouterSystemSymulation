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
        double[] rejected;
        double[] NumberOfReceivedPackages;//
        double[] NumberOfServedPackages;//
        double[] ProbabilityOfFailure;//
        string[] TableOfPriorities;//
        int NumberOfDistributions;
        double[] AverageServingTime;//
        double[] TotalServingTime;//
        double[] TotalOccupancyOfQueue;//
        double[] AverageOccupancyOfqueue;//
        int NumberOfQueues;
        int NumberOfStreams;
        double TimeOfSimulation;
        double CurrentTime;
        double AuxilaryTime;
        ProbabilityDistributions[] distribution;//
        FIFOQueue[] queue;//
        Heap  eventQueue;//
        Stream[] stream;//
        bool occupancyOfRouter;
        string inputDirectory;
        string outputDirectory;
        string systemName;
        int flowability;
        Package ProcessedPackage;
        double TotalOccupancyOfRouter;
        double AverageOccupancyOfRouter;

        public Simulation ()
        {
            TotalOccupancyOfRouter = 0;
            Package ProcessedPackage = new Package();
            NumberOfStreams = 0;
            GetData();
            TimeOfSimulation = CurrentTime =AuxilaryTime= 0;
            occupancyOfRouter = false;
            eventQueue=new Heap();
            NumberOfReceivedPackages = new double[NumberOfStreams + 1];
            NumberOfServedPackages = new double[NumberOfStreams + 1];
            ProbabilityOfFailure = new double[NumberOfStreams + 1];
            AverageServingTime = new double[NumberOfStreams + 1];
            TotalServingTime = new double[NumberOfStreams + 1];
            AverageOccupancyOfqueue = new double[NumberOfStreams];
            TotalOccupancyOfQueue = new double[NumberOfStreams];
            rejected = new double[NumberOfStreams +1];
            TotalOccupancyOfRouter = 0;
            AverageOccupancyOfRouter = 0;
            for (int i=0;i<=NumberOfStreams;i++)
            {
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
        public void fSimulation()
        {
            double previoustime=0;
            double CurrentTime = 0;
            Random rnd= new Random(DateTime.Now.Millisecond);
            while (TimeOfSimulation==0)
            {
                try
                {
                    Console.WriteLine("Prosze podac dodatni czas symulacji w ms: ");
                    TimeOfSimulation=double.Parse(Console.ReadLine());
                    if (TimeOfSimulation<=0) throw new Exception();

                }
                catch (Exception)
                {
                    Console.WriteLine("Prosze podaj inna liczbe ");
                }
                for (int i=0;i<NumberOfStreams;i++)
                {
                    double wait=Extractvalue(stream[i].GetNameOfWaitingDistribution(),NumberOfStreams);
                    eventQueue.Insert(Event.Coming(i, wait)); 
                }

            }
            while (CurrentTime<TimeOfSimulation)
            {
                Event TempEvent=eventQueue.Delete();
                previoustime = CurrentTime;
                CurrentTime=TempEvent.getKey();
                if (AuxilaryTime == 0)
                    AuxilaryTime = CurrentTime;
                else AuxilaryTime = CurrentTime - previoustime;
                for (int i=0;i<NumberOfStreams;i++)
                {
                    TotalOccupancyOfQueue[i] +=( queue[i].GetOccupancy()/queue[i].GetSize()) * AuxilaryTime;
                }
                if (occupancyOfRouter == true)
                    TotalOccupancyOfRouter += AuxilaryTime;

                
                  switch(TempEvent.GetKind())
                  {
                      case KindOfEvent.Coming:
                          {
                              double length=Extractvalue(stream[TempEvent.getStreamNumber()].GetNameOflengthDistribution(),NumberOfStreams);
                              Package TemporaryPackage = new Package(length,TempEvent.getKey());
                              NumberOfReceivedPackages[TempEvent.getStreamNumber()]++;
                              NumberOfReceivedPackages[NumberOfStreams ]++;

                              if (occupancyOfRouter==false)
                              {
                                  occupancyOfRouter = true;
                                  double processing = length/flowability;
                                  eventQueue.Insert(Event.GoingOutOfRouter(TempEvent.getStreamNumber(), CurrentTime+processing));
                                  ProcessedPackage = TemporaryPackage;
                     
                              }
                              else if (queue[stream[TempEvent.getStreamNumber()].bufor].occupancy + TemporaryPackage.GetSize() < queue[stream[TempEvent.getStreamNumber()].bufor].size)
                              {
                                  queue[stream[TempEvent.getStreamNumber()].bufor].SetPackage(TemporaryPackage);
                              }
                              else
                              {
                                  rejected[TempEvent.getStreamNumber()] += 1;
                              }
                              double interval = Extractvalue(stream[TempEvent.getStreamNumber()].GetNameOfWaitingDistribution(),NumberOfStreams);
                              eventQueue.Insert(Event.Coming(TempEvent.getStreamNumber(), CurrentTime + interval));
                                    
                          break;
                          }
                      case KindOfEvent.GoingOutOfRouter:
                          {
                              double ServingTime = CurrentTime - ProcessedPackage.GetComingTime();
                              TotalServingTime[TempEvent.getStreamNumber()] += ServingTime;
                              TotalServingTime[NumberOfStreams] += ServingTime;
                              NumberOfServedPackages[TempEvent.getStreamNumber()]++;
                              NumberOfServedPackages[NumberOfStreams] = NumberOfServedPackages[NumberOfStreams] + 1;
                              occupancyOfRouter = false;
                                  for (int i = 0; i < NumberOfQueues; i++)
                                  {
                                      if (queue[i].occupancy != 0)
                                      {
                                          occupancyOfRouter = true;
                                          ProcessedPackage = queue[i].MoveToStream();
                                          double length = Extractvalue(stream[i].GetNameOflengthDistribution(), NumberOfStreams);
                                          double processing = length/ flowability;
                                          eventQueue.Insert(Event.GoingOutOfRouter(i, CurrentTime + processing));
                                          break;
                                      }
                                  }
                              break;
                           }

                  }
                  
            }
            SumUp();
            double Av1 = rejected[NumberOfStreams] / NumberOfReceivedPackages[NumberOfStreams];
            Console.WriteLine("{0} oraz {1}", Av1, ProbabilityOfFailure[NumberOfStreams]);
            WriteResults();
            Console.WriteLine("Zakonczono symulacje");

        }

        public void GetData()
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
                    while (line.Length < 2 || line[0] == '#')
                    {
                        line = sr.ReadLine();
                    }
                    strings = line.Split(' ');
                    if (strings[0] == "SYSTEM" && strings[2] != "") systemName = strings[2];
                    else throw (new Exception("Zla nazwa systemu"));
                    #endregion
                    #region kanaly
                    line = "";
                    while (line.Length < 2 || line[0] == '#')
                    {
                        line = sr.ReadLine();
                    }
                    strings = line.Split(' ');
                    if (strings[0] == "PRZEPLYWNOSC" && strings[2] != "")
                    {
                        flowability = int.Parse(strings[2]);
                        flowability = flowability / 1000;
                    }
                    else throw (new Exception("Zla przeplywnosc"));
                    #endregion
                    #region kolejka
                    line = "";
                    while (line.Length < 2 || line[0] == '#')
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
                    queue = new FIFOQueue[NumberOfQueues];
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
                            size *= 8;
                        }
                        else throw (new Exception("Zla nazwa rozkladu i bufora"));
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
                    for (int i = 0; i < NumberOfDistributions; i++)
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
                           // bufor = ToNumberOfString(buforInString);
                            waitingTime = strings[8];
                            size = strings[11];
                        }
                        else throw (new Exception("Zle dane strumienia"));
                        for (int j = 0; j < TableOfPriorities.Length;j++ )
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

                    Console.WriteLine("Przeciagnij tu plik wyjsciowy i wcisnij ENTER...");
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

        public void SumUp()
        {
            for (int i = 0; i < NumberOfStreams; i++)
            {
                AverageOccupancyOfqueue[i] = TotalOccupancyOfQueue[i] / (TimeOfSimulation);
                AverageServingTime[i] = TotalServingTime[i] / NumberOfServedPackages[i];
                ProbabilityOfFailure[i] = (NumberOfReceivedPackages[i] - NumberOfServedPackages[i]) / NumberOfReceivedPackages[i];
            }
            AverageServingTime[NumberOfStreams ] = TotalServingTime[NumberOfStreams ] / NumberOfServedPackages[NumberOfStreams ]*NumberOfStreams;
            ProbabilityOfFailure[NumberOfStreams] = (NumberOfReceivedPackages[NumberOfStreams ] - NumberOfServedPackages[NumberOfStreams ]) / NumberOfReceivedPackages[NumberOfStreams ];
            AverageOccupancyOfRouter = TotalOccupancyOfRouter / TimeOfSimulation;

        }
        public void WriteResults()
        {
            StreamWriter routerSimulationResults = new StreamWriter(outputDirectory);

            routerSimulationResults.WriteLine("System = " + systemName);
            for (int i=0; i<NumberOfStreams;i++)
            {
                routerSimulationResults.WriteLine("Srednia zajetosc bufora {0} : " + AverageOccupancyOfqueue[i], TableOfPriorities[i]);
                routerSimulationResults.WriteLine("Sredni czas obslugi pakietu o priorytecie {0} : " + AverageServingTime[i], TableOfPriorities[i]);
                routerSimulationResults.WriteLine("Prawdopodobienstwo odrzucenia ze strumienia {0} : " + ProbabilityOfFailure[i], i);
            }

            routerSimulationResults.WriteLine();
            routerSimulationResults.WriteLine("Sredni czas obsugi pakietu : " + AverageServingTime[NumberOfStreams]);
            routerSimulationResults.WriteLine("Prawdopodobienstwo odrzucenia z Routera : " + ProbabilityOfFailure[NumberOfStreams ]);
            routerSimulationResults.WriteLine("Srednia zajetosc Routera : " + AverageOccupancyOfRouter);

            routerSimulationResults.Close();
        }
        public double Extractvalue (string name, int number)
        {
            Random rnd = new Random();
            double value;
            int j=0;
            for (int i=0;i<number;i++)
            {
                j=i;
                if (distribution[i].GetName() == name)
                    break;

            }
            return value = distribution[j].SetTime(rnd.NextDouble());
        }
    }
}
