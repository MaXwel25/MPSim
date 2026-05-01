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
    }
}
