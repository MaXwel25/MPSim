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

        public double BufferEnterTime { get; set; }   // время входа в буфер текущей фазы
        public double ProcessingStartTime { get; set; } // время начала обработки
        public double ProcessingEndTime { get; set; }   // время окончания обработки
        public double WaitTime { get; set; }            // w_i: время ожидания в буфере
        public double TotalWaitTime { get; set; }       // общее время ожидания по всем фазам
    }
}

