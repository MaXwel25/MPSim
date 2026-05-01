using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPSim.Models
{
    public class PhaseState
    {
        public int Index { get; set; }
        public int BufferSize { get; set; }      // задания в буфере (всего)
        public bool IsWorking { get; set; }      // занято ли ФУ
        public double RemainingTime { get; set; } // сколько осталось работать (s_i)

        // статистика для вывода в таблицы
        public double TotalIdleTime { get; set; } // время простоя
        public double TotalWaitTime { get; set; } // время ожидания в буфере
        public int JobsProcessed { get; set; }    // сколько прошло через ФУ
    }
}
