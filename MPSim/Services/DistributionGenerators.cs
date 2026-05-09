using System;

namespace MPSim.Services
{
    // генераторы случайных величин
    public static class DistributionGenerators
    {
        private static Random _random = new Random(42);

        public static void SetSeed(int seed)
        {
            _random = new Random(seed);
        }

        // экспоненциальное распределение (пуассоновский поток)
        // f(x) = λ * e^(-λx)
        public static double FuncExponential(double lambda)
        {
            // метод обратных функций
            double u = _random.NextDouble(); // от 0 до 1 вкл
            return -Math.Log(1.0 - u) / lambda;
        }

        // нормальное распределение (метод Бокса-Мюллера)
        public static double FuncNormal(double mean, double stdDev)
        {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();

            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * z;
        }

        // нормальное распределение, обрезанное до положительных значений
        public static double FuncTruncatedNormal(double mean, double stdDev)
        {
            double value;
            int maxAttempts = 1000;
            int attempts = 0;
            do
            {
                value = FuncNormal(mean, stdDev);
                attempts++;
            } while (value <= 0 && attempts < maxAttempts);

            return Math.Max(value, 0.001); // минимальное положительное значение
        }

        // равномерное распределение
        public static double FuncUniform(double min, double max)
        {
            return min + _random.NextDouble() * (max - min);
        }
    }
}