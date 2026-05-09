using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPSim.Models
{
    public class SimulationConfig
    {
        public int PhasesCount { get; set; } = 3;
        public int JobsCount { get; set; } = 100;
        public int NumRuns { get; set; } = 10;
        public double Lambda { get; set; } = 0.8;
        public double Mu { get; set; } = 1.0;
        public double Sigma { get; set; } = 0.2;
        public int Seed { get; set; } = 42;
        public int IntervalDistribution { get; set; } = 0;
        public int ProcessingDistribution { get; set; } = 0;
    }
}
