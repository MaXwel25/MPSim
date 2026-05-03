using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MPSim.Models;

namespace MPSim.Services
{
    // Класс для параметров распределения случайных величин
    public class DistributionParams
    {
        public string Type { get; set; } = "Uniform"; // "Uniform", "Exponential", "Normal"
        public double Min { get; set; } = 1.0;
        public double Max { get; set; } = 5.0;
        public double Lambda { get; set; } = 0.5;      // для экспоненциального
        public double Mean { get; set; } = 4.0;        // для нормального
        public double StdDev { get; set; } = 1.0;      // для нормального
    }

    public class SimulationEngine
    {
        // событие, вызываемое при обновлении кадра
        public Action<List<PhaseState>> OnUpdate;

        // история заданий для экспорта
        public List<Job> JobHistory { get; private set; } = new List<Job>();

        // общее время симуляции
        public int TotalSimulationTime { get; private set; }

        private Random _random; // инициализируется в RunAsync с учётом seed

        public async Task RunAsync(int k, int jobsCount, int tickDelayMs, int? seed, DistributionParams arrivalParams, DistributionParams processingParams, CancellationToken cancellationToken) // асинхронно!
        {
            // инициализация генератора случайных чисел с учётом seed
            _random = seed.HasValue ? new Random(seed.Value) : new Random();

            // очистка истории
            JobHistory.Clear();

            var states = new List<PhaseState>();
            var buffers = new Queue<Job>[k];
            var activeJobs = new Job[k];
            var phaseFreeTime = new double[k]; // free_time(i) — момент освобождения ФУ i

            // инициализация
            for (int i = 0; i < k; i++)
            {
                buffers[i] = new Queue<Job>();
                states.Add(new PhaseState { Index = i });
                phaseFreeTime[i] = 0;
            }

            int jobsCreated = 0;
            int tick = 0;
            int nextJobArrival = 0;

            // главный цикл симуляции
            while (jobsCreated < jobsCount || CheckBuffersNotEmpty(buffers, k))
            {
                cancellationToken.ThrowIfCancellationRequested();
                tick++;

                // генерация (случайные интервалы)
                if (jobsCreated < jobsCount && tick >= nextJobArrival)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var job = new Job(k)
                    {
                        Id = jobsCreated + 1,
                    };

                    // генерация s_i (от 2 до 6 тиков) — теперь с учётом выбранного распределения
                    for (int i = 0; i < k; i++)
                        job.ProcessingTimes[i] = (int)Math.Round(GenerateRandom(processingParams));

                    buffers[0].Enqueue(job);
                    jobsCreated++;

                    // следующее поступление через случайное время (1-4 тика)
                    nextJobArrival = tick + (int)Math.Round(GenerateRandom(arrivalParams));
                }

                // обработка всех фаз
                for (int i = k - 1; i >= 0; i--)
                {
                    var state = states[i];

                    if (state.IsWorking && activeJobs[i] != null)
                    {
                        state.RemainingTime--;

                        // завершение обработки на ФУ i
                        if (state.RemainingTime <= 0)
                        {
                            var job = activeJobs[i];
                            state.BufferSize = buffers[i].Count;
                        }
                        else
                        {
                            // ФУ простаивает
                            if (phaseFreeTime[i] < tick)
                                state.TotalIdleTime += 1;
                            state.BufferSize = 0;
                        }
                    }
                }

                // уведомление ui
                OnUpdate?.Invoke(states);

                // пауза для визуализации
                await Task.Delay(tickDelayMs, cancellationToken); // добавлен токен для кнопки стоп
            }
            TotalSimulationTime = tick;
        }

        private double GenerateRandom(DistributionParams p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            double result;
            switch (p.Type)
            {
                case "Uniform":
                    // равномерное распределение
                    result = p.Min + (p.Max - p.Min) * _random.NextDouble();
                    break;

                case "Exponential":
                    // экспоненциальное распределение с параметром λ
                    double lambda = Math.Max(p.Lambda, 1e-6); // Защита от деления на ноль
                    double u = 1.0 - _random.NextDouble();    // u ∈ (0, 1]
                    result = -Math.Log(u) / lambda;
                    break;

                case "Normal":
                    // нормальное распределение (преобразование Бокса-Мюллера)
                    double u1 = 1.0 - _random.NextDouble();
                    double u2 = 1.0 - _random.NextDouble();
                    result = p.Mean + p.StdDev * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                    break;

                default:
                    throw new ArgumentException($"Неподдерживаемый тип распределения: {p.Type}");
            }
            // ограничение выхода за границы, если заданы Min/Max
            if (p.Min < p.Max)
                result = Math.Max(p.Min, Math.Min(p.Max, result));

            return result;
        }

        private bool CheckBuffersNotEmpty(Queue<Job>[] buffers, int k) // проверка буфера
        {
            for (int i = 0; i < k; i++) if (buffers[i].Count > 0) return true;
            return false;
        }
    }
}
