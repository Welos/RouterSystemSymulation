using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router      
{
    class ProbabilityDistributions //  klasa obslugujaca rozklady dystrybuanty czasow oraz rozmiarow
    {
        private double lambda;     // wartosc lambdy danego rozkladu Poissona
        private string name;       // nazwa sluzaca rozroznieniu rozkladow

        public ProbabilityDistributions(string name, double lambda)
        {
            this.name = name;
            this.lambda = lambda;
        }

        public double SetTime(double x)
        {
            return ((-1) * Math.Log(1 - x) / lambda); //przeksztalcony wzor na rozklad wykladniczy
        }

        public string GetName() { return name; }
    }
}
