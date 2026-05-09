using System;
using System.Linq;
using MPSim.Models;
using MPSim.Services;

namespace MPSim.Core
{
    // движок имитационного моделирования
    public class SimulationEngine
    {
        private readonly SimulationConfig _config;
        private Conveyor _conveyor;
        private SimulationTask[] _tasks;

        // результаты симуляции (публичные свойства для UI)
        public double[] ThroughputPerTask { get; private set; }
        public double[] AvgWaitPerPhase { get; private set; }
        public double[] AvgIdlePerPhase { get; private set; }
        public double[] UtilizationPerPhase { get; private set; }
        public double TotalSimulationTime { get; private set; }

        public event Action<int, int>? OnTaskProcessed;
        public event Action<int>? OnRunCompleted;

        public SimulationEngine(SimulationConfig config)
        {
            _config = config;
            _conveyor = new Conveyor(config.PhasesCount);
        }

        public void Run()
        {
            int k = _config.PhasesCount;
            int n = _config.JobsCount;
            int runs = _config.NumRuns;

            // инициализация массивов результатов
            ThroughputPerTask = new double[n];
            AvgWaitPerPhase = new double[k];
            AvgIdlePerPhase = new double[k];
            UtilizationPerPhase = new double[k];

            double[] sumWait = new double[k];
            double[] sumIdle = new double[k];
            double[] sumUtil = new double[k];
            double[] sumThroughput = new double[n];

            for (int run = 0; run < runs; run++)
            {
                // детерминированный seed для каждого прогона
                DistributionGenerators.SetSeed(_config.Seed + run * 7919);

                _conveyor.Reset();
                _tasks = new SimulationTask[n];

                double currentTime = 0.0;
                for (int j = 0; j < n; j++)
                {
                    // генерация интервала поступления
                    double delta = GenerateIntervalTime();
                    currentTime += delta;

                    var task = new SimulationTask(j + 1, currentTime, k);

                    // генерация времён обработки для каждой фазы
                    for (int i = 0; i < k; i++)
                        task.ProcessingTimes[i] = GenerateProcessingTime();

                    // обработка задания конвейером
                    _conveyor.ProcessTask(task);
                    _tasks[j] = task;

                    OnTaskProcessed?.Invoke(j + 1, n);
                }

                TotalSimulationTime = _conveyor.GetFinishTime();

                // накопление метрик для усреднения
                for (int i = 0; i < k; i++)
                {
                    var fu = _conveyor.GetFunctionalUnit(i);
                    sumWait[i] += fu.GetAverageWaitTime();
                    sumIdle[i] += fu.GetAverageIdleTime();
                    sumUtil[i] += fu.GetUtilization(TotalSimulationTime);
                }

                // пропускная способность
                for (int j = 0; j < n; j++)
                    sumThroughput[j] += (j + 1.0) / _tasks[j].FinishTimes[k - 1];

                OnRunCompleted?.Invoke(run + 1);
            }

            // усреднение по прогонам
            for (int i = 0; i < k; i++)
            {
                AvgWaitPerPhase[i] = sumWait[i] / runs;
                AvgIdlePerPhase[i] = sumIdle[i] / runs;
                UtilizationPerPhase[i] = sumUtil[i] / runs;
            }
            for (int j = 0; j < n; j++)
                ThroughputPerTask[j] = sumThroughput[j] / runs;
        }

        // генерация интервала между поступлениями (выбор распределения)
        private double GenerateIntervalTime()
        {
            return _config.IntervalDistribution switch
            {
                0 => DistributionGenerators.FuncExponential(_config.Lambda),
                1 => DistributionGenerators.FuncUniform(_config.Lambda * 0.5, _config.Lambda * 1.5),
                2 => DistributionGenerators.FuncTruncatedNormal(1.0 / _config.Lambda, 0.2),
                _ => DistributionGenerators.FuncExponential(_config.Lambda)
            };
        }

        // генерация времени обработки на фазе (выбор распределения
        private double GenerateProcessingTime()
        {
            return _config.ProcessingDistribution switch
            {
                0 => DistributionGenerators.FuncTruncatedNormal(_config.Mu, _config.Sigma),
                1 => DistributionGenerators.FuncUniform(_config.Mu - _config.Sigma, _config.Mu + _config.Sigma),
                2 => DistributionGenerators.FuncExponential(1.0 / _config.Mu),
                _ => DistributionGenerators.FuncTruncatedNormal(_config.Mu, _config.Sigma)
            };
        }

        public SimulationTask? GetTask(int id) =>
            _tasks?.FirstOrDefault(t => t?.Id == id);

        public SimulationTask[] GetAllTasks() =>
            _tasks?.ToArray() ?? Array.Empty<SimulationTask>();
    }
}