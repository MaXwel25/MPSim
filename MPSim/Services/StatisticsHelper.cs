using System;
using System.Collections.Generic;
using System.Linq;

namespace MPSim.Services
{
    public static class StatisticsHelper
    {
        public static double Percentile(IEnumerable<double> data, double p)
        {
            var sorted = data.OrderBy(x => x).ToArray();
            if (sorted.Length == 0) return 0;
            if (p <= 0) return sorted.First();
            if (p >= 1) return sorted.Last();

            double index = p * (sorted.Length - 1);
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);
            if (lower == upper) return sorted[lower];

            double fraction = index - lower;
            return sorted[lower] + fraction * (sorted[upper] - sorted[lower]);
        }

        public static double Variance(this IEnumerable<double> data)
        {
            var arr = data.ToArray();
            if (arr.Length < 2) return 0;
            double mean = arr.Average();
            return arr.Sum(x => Math.Pow(x - mean, 2)) / (arr.Length - 1);
        }

        public static double StdDev(this IEnumerable<double> data) => Math.Sqrt(Variance(data));

        public static double CoefficientOfVariation(this IEnumerable<double> data)
        {
            var arr = data.ToArray();
            double mean = arr.Average();
            return mean > 1e-9 ? StdDev(arr) / mean : 0;
        }

        public static double ConfidenceMargin(this IEnumerable<double> data, double confidenceLevel = 0.95)
        {
            var arr = data.ToArray();
            if (arr.Length < 2) return 0;

            double std = StdDev(arr);
            double tValue = StudentTValue(arr.Length - 1, confidenceLevel);
            return tValue * std / Math.Sqrt(arr.Length);
        }

        // таблица критических значений t-Стьюдента для 95% доверительного уровня
        private static readonly Dictionary<int, double> _tTable95 = new()
        {
            {1, 12.706}, {2, 4.303}, {3, 3.182}, {4, 2.776}, {5, 2.571},
            {6, 2.447}, {7, 2.365}, {8, 2.306}, {9, 2.262}, {10, 2.228},
            {15, 2.131}, {20, 2.086}, {25, 2.060}, {30, 2.042}, {40, 2.021},
            {60, 2.000}, {120, 1.980}, {int.MaxValue, 1.960}
        };

        public static double StudentTValue(int degreesOfFreedom, double confidenceLevel = 0.95)
        {
            if (Math.Abs(confidenceLevel - 0.95) < 1e-6)
            {
                var keys = _tTable95.Keys.OrderBy(k => k).ToArray();
                foreach (var key in keys)
                    if (degreesOfFreedom <= key) return _tTable95[key];
                return _tTable95[int.MaxValue];
            }
            // для других уровней (заглушка)
            return 1.96;
        }
    }
}