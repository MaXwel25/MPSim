using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MPSim.Models;

namespace MPSim.Services
{
    public class ExportService
    {

        public static void ExportFullReport(string filePath, List<PhaseState> states, List<Job> jobs, SimulationParameters parameters, int totalTime)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Параметры запуска симуляции:");
            sb.AppendLine($"Количество фаз (k);{parameters.PhasesCount}");
            sb.AppendLine($"Количество заданий (N);{parameters.JobsCount}");
            sb.AppendLine($"Seed;{parameters.RandomSeed}");
            sb.AppendLine($"Распределение интервалов;{parameters.ArrivalDistType}");
            sb.AppendLine($"Распределение обработки;{parameters.ProcessingDistType}");
            sb.AppendLine($"Общее время симуляции;{totalTime}");
            sb.AppendLine();

            // среднее время выполнения
            double avgTotalTime = jobs.Count > 0 ? jobs.Average(j => j.TotalTime) : 0;
            sb.AppendLine("Эффективность:");
            sb.AppendLine($"Среднее время выполнения задания;{avgTotalTime:F2}");
            sb.AppendLine($"Пропускная способность (зад/тик);{(jobs.Count > 0 ? jobs.Count / (double)totalTime : 0):F4}");
            sb.AppendLine();

            // статистика фаз
            sb.AppendLine("Статистика фаз:");
            sb.AppendLine("Фаза;Nᵢ;Σwᵢⱼ;w̄ᵢ;Σidleᵢⱼ;idlēᵢ;Uᵢ(%)");

            foreach (var state in states)
            {
                double utilization = state.GetUtilization(totalTime) * 100;
                sb.AppendLine($"{state.Index + 1};" +
                             $"{state.JobsProcessed};" +
                             $"{state.TotalWaitTime:F2};" +
                             $"{state.AverageWaitTime:F2};" +
                             $"{state.TotalIdleTime:F2};" +
                             $"{state.AverageIdleTime:F2};" +
                             $"{utilization:F1}");
            }
            sb.AppendLine();

            // детали
            sb.AppendLine("Детали заданий:");
            sb.AppendLine("ID;T_j;Sum w_iⱼ;Sum s_ij");

            int count = Math.Min(50, jobs.Count);
            for (int j = 0; j < count; j++)
            {
                var job = jobs[j];
                double sumS = 0;
                double sumW = 0;
                for (int i = 0; i < job.ProcessingTimes.Length; i++)
                {
                    sumS += job.ProcessingTimes[i];
                    sumW += job.WaitTimes[i];
                }
                sb.AppendLine($"{job.Id};{job.TotalTime:F2};{sumW:F2};{sumS:F2}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}