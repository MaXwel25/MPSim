using System;
using System.Collections.Generic;
using System.Linq;
using MPSim.Models;
using MPSim.Services;

namespace MPSim.Core
{
    public class SimulationEngine
    {
        private readonly SimulationConfig _config;
        private Conveyor _conveyor;
        private SimulationTask[] _tasks;

        // результаты для UI (усреднённые)
        public double[] ThroughputPerTask { get; private set; }
        public double[] AvgWaitPerPhase { get; private set; }
        public double[] AvgIdlePerPhase { get; private set; }
        public double[] UtilizationPerPhase { get; private set; }
        public double TotalSimulationTime { get; private set; }

        // история по прогонам для доверительных интервалов
        private readonly List<double[]> _waitPerRun = new();
        private readonly List<double[]> _idlePerRun = new();
        private readonly List<double[]> _utilPerRun = new();
        private readonly List<double> _throughputPerRun = new();
        private readonly List<double> _makespanPerRun = new();

        // детальные данные по задачам для распределений
        private readonly List<double> _allWaitTimes = new();
        private readonly List<double> _allServiceTimes = new();
        private readonly List<double> _allSojournTimes = new();
        private readonly List<(int Phase, double Time)> _queueEvents = new();

        public event Action<int, int>? OnTaskProcessed;
        public event Action<int>? OnRunCompleted;

        public SimulationEngine(SimulationConfig config)
        {
            _config = config;
            _conveyor = new Conveyor(config.PhasesCount);
        }

        public SimulationConfig GetConfig() => _config;

        public void Run(CancellationToken token = default)
        {
            int k = _config.PhasesCount;
            int n = _config.JobsCount;
            int runs = _config.NumRuns;

            ThroughputPerTask = new double[n];
            AvgWaitPerPhase = new double[k];
            AvgIdlePerPhase = new double[k];
            UtilizationPerPhase = new double[k];

            double[] sumWait = new double[k];
            double[] sumIdle = new double[k];
            double[] sumUtil = new double[k];
            double[] sumThroughput = new double[n];

            // очистка коллекций перед новым запуском
            _allWaitTimes.Clear();
            _allServiceTimes.Clear();
            _allSojournTimes.Clear();
            _queueEvents.Clear();
            _waitPerRun.Clear();
            _idlePerRun.Clear();
            _utilPerRun.Clear();
            _throughputPerRun.Clear();
            _makespanPerRun.Clear();

            for (int run = 0; run < runs; run++)
            {
                token.ThrowIfCancellationRequested();
                DistributionGenerators.SetSeed(_config.Seed + run * 7919);

                _conveyor.Reset();
                _tasks = new SimulationTask[n];

                double currentTime = 0.0;
                int tasksProcessed = 0;

                for (int j = 0; j < n; j++)
                {
                    if (token.IsCancellationRequested) break;

                    double delta = GenerateIntervalTime();
                    currentTime += delta;

                    var task = new SimulationTask(j + 1, currentTime, k);

                    for (int i = 0; i < k; i++)
                        task.ProcessingTimes[i] = GenerateProcessingTime();

                    _conveyor.ProcessTask(task);
                    _tasks[j] = task;
                    tasksProcessed++;

                    // сбор детальных метрик
                    CollectTaskMetrics(task, k);

                    OnTaskProcessed?.Invoke(j + 1, n);
                }

                if (tasksProcessed == 0) continue;
                TotalSimulationTime = _conveyor.GetFinishTime();

                // накопление для усреднения
                for (int i = 0; i < k; i++)
                {
                    var fu = _conveyor.GetFunctionalUnit(i);
                    sumWait[i] += fu.GetAverageWaitTime();
                    sumIdle[i] += fu.GetAverageIdleTime();
                    sumUtil[i] += fu.GetUtilization(TotalSimulationTime);
                }

                for (int j = 0; j < tasksProcessed; j++)
                {
                    if (_tasks[j]?.FinishTimes?.Length == k && _tasks[j].FinishTimes[k - 1] > 0)
                        sumThroughput[j] += (j + 1.0) / _tasks[j].FinishTimes[k - 1];
                }

                // сохранение метрик прогона
                var runWait = new double[k];
                var runIdle = new double[k];
                var runUtil = new double[k];

                for (int i = 0; i < k; i++)
                {
                    var fu = _conveyor.GetFunctionalUnit(i);
                    runWait[i] = fu.GetAverageWaitTime();
                    runIdle[i] = fu.GetAverageIdleTime();
                    runUtil[i] = fu.GetUtilization(TotalSimulationTime);
                }

                _waitPerRun.Add(runWait);
                _idlePerRun.Add(runIdle);
                _utilPerRun.Add(runUtil);
                _throughputPerRun.Add(TotalSimulationTime > 0 ? n / TotalSimulationTime : 0);
                _makespanPerRun.Add(TotalSimulationTime);

                OnRunCompleted?.Invoke(run + 1);
            }

            // финальное усреднение
            for (int i = 0; i < k; i++)
            {
                AvgWaitPerPhase[i] = sumWait[i] / runs;
                AvgIdlePerPhase[i] = sumIdle[i] / runs;
                UtilizationPerPhase[i] = sumUtil[i] / runs;
            }
            for (int j = 0; j < n; j++)
                ThroughputPerTask[j] = sumThroughput[j] / runs;
        }

        private void CollectTaskMetrics(SimulationTask task, int k)
        {
            for (int i = 0; i < k; i++)
            {
                _allWaitTimes.Add(task.WaitTimes[i]);
                _allServiceTimes.Add(task.ProcessingTimes[i]);

                // событие для временного ряда очереди
                if (task.WaitTimes[i] > 0)
                    _queueEvents.Add((i + 1, task.StartTimes[i]));
            }
            _allSojournTimes.Add(task.TotalTime);
        }

        // для экспорта
        public double[] GetWaitHistoryForPhase(int phaseIndex) =>
            _tasks?.Where(t => t != null).Select(t => t.WaitTimes[phaseIndex]).ToArray() ?? Array.Empty<double>();

        public double[] GetServiceTimeHistoryForPhase(int phaseIndex) =>
            _tasks?.Where(t => t != null).Select(t => t.ProcessingTimes[phaseIndex]).ToArray() ?? Array.Empty<double>();

        public double[] GetAllWaitTimes() => _allWaitTimes.ToArray();
        public double[] GetAllServiceTimes() => _allServiceTimes.ToArray();
        public double[] GetAllSojournTimes() => _allSojournTimes.ToArray();

        public (int Phase, double Time)[] GetQueueEventTimeline() => _queueEvents.ToArray();

        public double[] GetThroughputPerRun() => _throughputPerRun.ToArray();
        public double[] GetMakespanPerRun() => _makespanPerRun.ToArray();
        public double[] GetAvgWaitPerRun() => _waitPerRun.Select(r => r.Average()).ToArray();
        public double[] GetAvgIdlePerRun() => _idlePerRun.Select(r => r.Average()).ToArray();

        // средняя утилизация по прогонам
        public double[] GetAvgUtilPerRun() => _utilPerRun.Select(r => r.Average()).ToArray();

        // дополнительные методы (тестовые)
        public double[] GetUtilPerRun(int phaseIndex) =>
            _utilPerRun.Select(r => phaseIndex < r.Length ? r[phaseIndex] : 0.0).ToArray();

        public double GetCurrentThroughput()
        {
            if (TotalSimulationTime == 0 || _tasks == null) return 0.0;
            int processed = _tasks.Count(t => t != null && t.FinishTimes[^1] > 0);
            return processed / TotalSimulationTime;
        }

        private double GenerateIntervalTime()
        {
            double interval = _config.IntervalDistribution switch
            {
                0 => DistributionGenerators.FuncExponential(_config.Lambda),
                1 => DistributionGenerators.FuncUniform(_config.Lambda * 0.5, _config.Lambda * 1.5),
                2 => DistributionGenerators.FuncTruncatedNormal(1.0 / _config.Lambda, 0.2),
                _ => DistributionGenerators.FuncExponential(_config.Lambda)
            };
            return Math.Max(0.0001, interval);
        }

        private double GenerateProcessingTime()
        {
            double time = _config.ProcessingDistribution switch
            {
                0 => DistributionGenerators.FuncTruncatedNormal(_config.Mu, _config.Sigma),
                1 => DistributionGenerators.FuncUniform(_config.Mu - _config.Sigma, _config.Mu + _config.Sigma),
                2 => DistributionGenerators.FuncExponential(1.0 / _config.Mu),
                _ => DistributionGenerators.FuncTruncatedNormal(_config.Mu, _config.Sigma)
            };
            return Math.Max(0, time);
        }

        public SimulationTask? GetTask(int id) => _tasks?.FirstOrDefault(t => t?.Id == id);
        public SimulationTask[] GetAllTasks() => _tasks?.ToArray() ?? Array.Empty<SimulationTask>();

        public class PhaseDetailedStats
        {
            public int PhaseId { get; set; }
            public double Utilization { get; set; }
            public double AvgWaitTime { get; set; }
            public double AvgIdleTime { get; set; }
            public int TasksProcessed { get; set; }
            public double TotalBusyTime { get; set; }
            public double TotalWaitAccumulated { get; set; }
            public double TotalIdleAccumulated { get; set; }

            public double WaitP95 { get; set; }
            public double ServiceTimeCV { get; set; }
            public int MaxQueueLength { get; set; }
        }

        public PhaseDetailedStats[] GetDetailedPhaseStats()
        {
            var stats = new PhaseDetailedStats[_config.PhasesCount];
            for (int i = 0; i < _config.PhasesCount; i++)
            {
                var fu = _conveyor.GetFunctionalUnit(i);
                var waitHistory = GetWaitHistoryForPhase(i);
                var serviceHistory = GetServiceTimeHistoryForPhase(i);

                stats[i] = new PhaseDetailedStats
                {
                    PhaseId = i + 1,
                    Utilization = fu.GetUtilization(TotalSimulationTime),
                    AvgWaitTime = fu.GetAverageWaitTime(),
                    AvgIdleTime = fu.GetAverageIdleTime(),
                    TasksProcessed = fu.TasksProcessed,
                    TotalBusyTime = fu.TotalBusyTime,
                    TotalWaitAccumulated = fu.TotalWaitAccumulated,
                    TotalIdleAccumulated = fu.TotalIdleAccumulated,
                    WaitP95 = StatisticsHelper.Percentile(waitHistory, 0.95),
                    ServiceTimeCV = StatisticsHelper.CoefficientOfVariation(serviceHistory),
                    MaxQueueLength = EstimateMaxQueueLength(i)
                };
            }
            return stats;
        }

        private int EstimateMaxQueueLength(int phaseIndex)
        {
            var waits = GetWaitHistoryForPhase(phaseIndex);
            return waits.Count(w => w > 0);
        }
    }
}