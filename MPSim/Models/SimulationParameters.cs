using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPSim.Models
{
    public class SimulationParameters
    {
        // основные параметры
        public int PhasesCount { get; set; } = 4;           // k — количество фаз
        public int JobsCount { get; set; } = 30;            // N — количество заданий
        public int TickDelayMs { get; set; } = 150;         // задержка визуализации

        // генерация случайных чисел
        public bool UseRandomSeed { get; set; } = false;
        public int RandomSeed { get; set; } = 42;

        // распределение интервалов поступления
        public string ArrivalDistType { get; set; } = "Uniform";
        public double ArrivalMin { get; set; } = 1.0;
        public double ArrivalMax { get; set; } = 5.0;
        public double ArrivalLambda { get; set; } = 0.5;


        // распределение времени обработки (s_i)
        public string ProcessingDistType { get; set; } = "Uniform";
        public double ProcessingMin { get; set; } = 2.0;
        public double ProcessingMax { get; set; } = 6.0;
        public double ProcessingMean { get; set; } = 4.0;
        public double ProcessingStdDev { get; set; } = 1.0;
    }
}

