using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router
{
    class ProbabilityDistributions
    {
        private double lambda;
        private string name;

        public ProbabilityDistributions(string name, double lambda)
        {
            this.name = name;
            this.lambda = lambda;
        }

        public double SetTime(double x)
        {
            return ((-1) * Math.Log(1 - x) / lambda); //przeksztalcony wzor na Rozklad wykladniczy
        }

        public string GetName() { return name; }
    }
}
