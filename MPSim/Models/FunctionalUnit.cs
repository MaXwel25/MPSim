using System;

namespace MPSim.Models
{
    public class FunctionalUnit
    {
        public int Id { get; set; }
        public double FreeTime { get; private set; } // когда ФУ освободится
        public double TotalBusyTime { get; private set; } // общее время работы
        public double TotalWaitAccumulated { get; private set; } // накопленное время ожидания
        public double TotalIdleAccumulated { get; private set; } // накопленное время простоя
        public int TasksProcessed { get; private set; }

        public FunctionalUnit(int id)
        {
            Id = id;
            FreeTime = 0.0;
            TotalBusyTime = 0.0;
            TotalWaitAccumulated = 0.0;
            TotalIdleAccumulated = 0.0;
            TasksProcessed = 0;
        }

        // обработка задания на фазе
        public void ProcessTask(double readyTime, double processingTime,
            out double startTime, out double finishTime, out double waitTime, out double idleTime)
        {
            // startTime = max(readyTime, freeTime)
            startTime = Math.Max(readyTime, FreeTime);

            // waitTime = max(0, freeTime - readyTime)
            waitTime = Math.Max(0.0, FreeTime - readyTime);

            // idleTime = max(0, readyTime - freeTime)
            idleTime = Math.Max(0.0, readyTime - FreeTime);

            // finishTime = startTime + processingTime
            finishTime = startTime + processingTime;

            // обновляем состояние ФУ
            FreeTime = finishTime;
            TotalBusyTime += processingTime;
            TotalWaitAccumulated += waitTime;
            TotalIdleAccumulated += idleTime;
            TasksProcessed++;
        }

        // коэффициент загрузки
        public double GetUtilization(double totalTime)
        {
            return totalTime > 0 ? TotalBusyTime / totalTime : 0.0;
        }

        // среднее время ожидания
        public double GetAverageWaitTime()
        {
            return TasksProcessed > 0 ? TotalWaitAccumulated / TasksProcessed : 0.0;
        }

        // среднее время простоя
        public double GetAverageIdleTime()
        {
            return TasksProcessed > 0 ? TotalIdleAccumulated / TasksProcessed : 0.0;
        }

        public void Reset()
        {
            FreeTime = 0.0;
            TotalBusyTime = 0.0;
            TotalWaitAccumulated = 0.0;
            TotalIdleAccumulated = 0.0;
            TasksProcessed = 0;
        }
    }
}