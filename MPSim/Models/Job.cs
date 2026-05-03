using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPSim.Models
{
    public class Job
    {
        public int Id { get; set; }
        public int[] ProcessingTimes { get; set; } // вектор s = (s1, s2, ..., s_k)

        public double[] WaitTimes { get; set; }        // w_ij — время ожидания
        public double[] IdleTimes { get; set; }        // idle_ij — простой ФУ i из-за этого задания
        public double[] StartTimes { get; set; }       // start
        public double[] FinishTimes { get; set; }      // finish

        //
        public double TotalTime { get; set; }

        public Job(int k)
        {
            ProcessingTimes = new int[k];
            WaitTimes = new double[k];
            IdleTimes = new double[k];
            StartTimes = new double[k];
            FinishTimes = new double[k];
            TotalTime = 0;
        }
    }
}

