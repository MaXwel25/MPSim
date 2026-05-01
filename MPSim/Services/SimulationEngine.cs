using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MPSim.Models;

namespace MPSim.Services
{
    public class SimulationEngine
    {
        // событие, вызываемое при обновлении кадра
        public Action<List<PhaseState>> OnUpdate;

        private readonly Random _random = new Random(42); // пока фиксированный Seed

        // добавка токена для 
        public async Task RunAsync(int k, int jobsCount, int tickDelayMs, CancellationToken cancellationToken) // асинхронно!
        {
            var states = new List<PhaseState>();
            var buffers = new Queue<Job>[k];

            // инициализация
            for (int i = 0; i < k; i++)
            {
                buffers[i] = new Queue<Job>();
                states.Add(new PhaseState { Index = i });
            }

            int jobsCreated = 0;
            int tick = 0;
            int nextJobArrival = 0;

            // главный цикл симуляции
            while (jobsCreated < jobsCount || CheckBuffersNotEmpty(buffers, k))
            {
                tick++;

                // генерация (случайные интервалы)
                if (jobsCreated < jobsCount && tick >= nextJobArrival)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var job = new Job
                    {
                        Id = jobsCreated + 1,
                        ProcessingTimes = new int[k]
                    };

                    // генерация s_i (от 2 до 6 тиков)
                    for (int i = 0; i < k; i++) job.ProcessingTimes[i] = _random.Next(2, 7);

                    buffers[0].Enqueue(job);
                    jobsCreated++;

                    // следующее поступление через случайное время (1-4 тика)
                    nextJobArrival = tick + _random.Next(1, 5);
                }

                // обработка всех фаз
                for (int i = k - 1; i >= 0; i--)
                {
                    var state = states[i];
                    var fuBusy = state.IsWorking;

                    if (fuBusy)
                    {
                        state.RemainingTime--;

                        // если ФУ освободилось
                        if (state.RemainingTime <= 0)
                        {
                            state.IsWorking = false;
                            state.JobsProcessed++;

                            // перемещаем задание в следующий буфер (если не последняя фаза)
                            if (i < k - 1)
                            {
                                // пока не реализовано
                            }
                        }
                    }
                    else
                    {
                        // если свободно, то пробуем взять из буфера
                        if (buffers[i].Count > 0)
                        {
                            // ВРЕМЯ УПРОЩЕНО (временно)
                            var job = buffers[i].Dequeue();
                            state.IsWorking = true;
                            state.RemainingTime = job.ProcessingTimes[i];
                            state.BufferSize = buffers[i].Count;

                        }
                        else
                        {
                            state.TotalIdleTime++; // простой
                            state.BufferSize = 0;
                        }
                    }
                }

                // уведомление ui
                OnUpdate?.Invoke(states);

                // пауза для визуализации
                await Task.Delay(tickDelayMs, cancellationToken); // добавлен токен для кнопки стоп
            }
        }

        private bool CheckBuffersNotEmpty(Queue<Job>[] buffers, int k) // проверка буфера
        {
            for (int i = 0; i < k; i++) if (buffers[i].Count > 0) return true;
            return false;
        }
    }
}
