using System;
using System.Linq;

namespace MPSim.Models
{
    // конвейерная система - объединяет k ФУ
    public class Conveyor
    {
        public int PhaseCount { get; private set; }
        private readonly FunctionalUnit[] _functionalUnits;

        public Conveyor(int phaseCount)
        {
            PhaseCount = phaseCount;
            _functionalUnits = Enumerable.Range(0, phaseCount)
                .Select(i => new FunctionalUnit(i + 1))
                .ToArray();
        }

        // обработка задания через все фазы конвейера
        public void ProcessTask(SimulationTask task)
        {
            double readyTime = task.ArrivalTime;

            for (int i = 0; i < PhaseCount; i++)
            {
                _functionalUnits[i].ProcessTask(
                    readyTime,
                    task.ProcessingTimes[i],
                    out double startTime,
                    out double finishTime,
                    out double waitTime,
                    out double idleTime);

                task.StartTimes[i] = startTime;
                task.FinishTimes[i] = finishTime;
                task.WaitTimes[i] = waitTime;
                task.IdleTimes[i] = idleTime;

                // готовность для следующей фазы = завершение текущей (лчевидно)
                readyTime = finishTime;
            }
        }

        public FunctionalUnit GetFunctionalUnit(int phaseIndex)
        {
            if (phaseIndex < 0 || phaseIndex >= PhaseCount)
                throw new ArgumentOutOfRangeException(nameof(phaseIndex));
            return _functionalUnits[phaseIndex];
        }

        public FunctionalUnit[] GetAllFunctionalUnits() => _functionalUnits.ToArray();

        // время завершения последнего задания
        public double GetFinishTime()
        {
            return _functionalUnits.Max(fu => fu.FreeTime);
        }

        public void Reset()
        {
            foreach (var fu in _functionalUnits)
                fu.Reset();
        }
    }
}