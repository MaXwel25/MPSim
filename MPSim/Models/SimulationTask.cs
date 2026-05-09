using System;

namespace MPSim.Models
{
    // модель задания
    public class SimulationTask
    {
        public int Id { get; set; }
        public double ArrivalTime { get; set; }
        public double[] ProcessingTimes { get; set; } // s_i - время выполнения i-й фазы
        public double[] StartTimes { get; set; }      // моменты начала обработки
        public double[] FinishTimes { get; set; }     // моменты завершения обработки
        public double[] WaitTimes { get; set; }       // времена ожидания в буферах
        public double[] IdleTimes { get; set; }       // времена простоя ФУ

        public int PhaseCount => ProcessingTimes?.Length ?? 0;

        public SimulationTask(int id, double arrivalTime, int phaseCount)
        {
            Id = id;
            ArrivalTime = arrivalTime;
            ProcessingTimes = new double[phaseCount];
            StartTimes = new double[phaseCount];
            FinishTimes = new double[phaseCount];
            WaitTimes = new double[phaseCount];
            IdleTimes = new double[phaseCount];
        }

        /// <summary>
        /// Общее время выполнения задания (формула 5)
        /// </summary>
        public double TotalTime => FinishTimes[PhaseCount - 1] - ArrivalTime;

        /// <summary>
        /// Суммарное время ожидания
        /// </summary>
        public double TotalWaitTime => WaitTimes.Sum();
    }
}