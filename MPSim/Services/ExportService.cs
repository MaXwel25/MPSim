using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MPSim.Models;

namespace MPSim.Services
{
    public class ExportService
    {
        public static void ExportAllResults(string filePath, List<Job> jobs, int totalTime)
        {
            var sb = new StringBuilder();

            // заголовок
            sb.AppendLine("ID;Фаза;Вход в буфер;Ожидание (w_i);Всего ожиданий;Время обработки (s_i)");

            // данные
            foreach (var job in jobs)
            {
                // выводим сводную информацию по каждому заданию
                sb.AppendLine($"{job.Id};-;{job.BufferEnterTime:F2};{job.WaitTime:F2};{job.TotalWaitTime:F2};{job.ProcessingTimes?.Length}");
            }

            sb.AppendLine($"\nОбщее время симуляции: {totalTime}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}