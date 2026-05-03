using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace MPSim.Models
{
    public class PhaseState
    {
        public int Index { get; set; }              // номер фазы (0...k-1)

        // текущее состояние для визуализации
        public int BufferSize { get; set; }         // заданий в буфере
        public bool IsWorking { get; set; }         // занято ли ФУ
        public double RemainingTime { get; set; }   // осталось времени обработки
        public double TotalWaitTime { get; set; }
        public double TotalIdleTime { get; set; }
        public int JobsProcessed { get; set; }      // N_i — количество обработанных заданий
        public double AverageWaitTime => JobsProcessed > 0 ? TotalWaitTime / JobsProcessed : 0;
        public double AverageIdleTime => JobsProcessed > 0 ? TotalIdleTime / JobsProcessed : 0;
        public double GetUtilization(double totalTime) =>
            totalTime > 0 ? (totalTime - TotalIdleTime) / totalTime : 0;
    }
}
