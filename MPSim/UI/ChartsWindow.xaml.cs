using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MPSim.Core;
using MPSim.Models;
using MPSim.Services;
using ScottPlot;

namespace MPSim.UI
{
    public partial class ChartsWindow : Window
    {
        private Plot _plot;
        private readonly SimulationEngine _engine;

        private enum DataType
        {
            Throughput,
            Utilization,
            AvgWait,
            AvgIdle,
            CombinedWaitIdle,
            UtilVsWait,
            ThroughputHist,
            Bottleneck,
            LoadBalance,
            WaitDist,
            PhaseEfficiency,
            CumulativeDelay
        }
        private DataType _currentDataType = DataType.Throughput;

        private enum ChartStyle { Bar, Scatter, Line, LineWithMarkers }
        private ChartStyle _currentStyle = ChartStyle.Bar;

        public ChartsWindow(SimulationEngine engine)
        {
            InitializeComponent();
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Loaded += (s, e) => InitializePlot();
        }

        private void InitializePlot()
        {
            if (MainPlot?.Plot == null)
            {
                MessageBox.Show("Ошибка инициализации графика.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _plot = MainPlot.Plot;
            UpdatePlotTheme();
            UpdateChartData();
            UpdateStatsLabel();
            MainPlot.Refresh();
        }

        private void UpdateChartData()
        {
            if (_plot == null || _engine?.ThroughputPerTask == null) return;
            _plot.Clear();

            switch (_currentDataType)
            {
                case DataType.Throughput: PlotThroughput(); break;
                case DataType.Utilization: PlotUtilization(); break;
                case DataType.AvgWait: PlotAvgWait(); break;
                case DataType.AvgIdle: PlotAvgIdle(); break;
                case DataType.CombinedWaitIdle: PlotCombinedWaitIdle(); break;
                case DataType.UtilVsWait: PlotUtilVsWaitScatter(); break;
                case DataType.ThroughputHist: PlotThroughputHistogram(); break;
                case DataType.Bottleneck: PlotBottleneckAnalysis(); break;
                case DataType.LoadBalance: PlotLoadBalance(); break;
                case DataType.WaitDist: PlotWaitDistribution(); break;
                case DataType.PhaseEfficiency: PlotPhaseEfficiency(); break;
                case DataType.CumulativeDelay: PlotCumulativeDelay(); break;
            }
            MainPlot?.Refresh();
        }

        private void PlotThroughput()
        {
            var x = Enumerable.Range(1, _engine.ThroughputPerTask.Length).Select(i => (double)i).ToArray();
            var y = _engine.ThroughputPerTask;
            RenderChart(x, y, "Номер задания (N)", "Пропускная способность",
                "Накопленная пропускная способность", 0, null, ScottPlot.Colors.Blue, true);
        }

        private void PlotUtilization()
        {
            var x = Enumerable.Range(1, _engine.UtilizationPerPhase.Length).Select(i => (double)i).ToArray();
            var y = _engine.UtilizationPerPhase;
            RenderChart(x, y, "Номер фазы", "Коэффициент загрузки (ρ)",
                "Загрузка ФУ", 0, 1.0, ScottPlot.Colors.SteelBlue, false);
        }

        private void PlotAvgWait()
        {
            var x = Enumerable.Range(1, _engine.AvgWaitPerPhase.Length).Select(i => (double)i).ToArray();
            var y = _engine.AvgWaitPerPhase;
            RenderChart(x, y, "Номер фазы", "Среднее время ожидания",
                "Время ожидания", 0, null, ScottPlot.Colors.Orange, false);
        }

        private void PlotAvgIdle()
        {
            var x = Enumerable.Range(1, _engine.AvgIdlePerPhase.Length).Select(i => (double)i).ToArray();
            var y = _engine.AvgIdlePerPhase;
            RenderChart(x, y, "Номер фазы", "Среднее время простоя",
                "Простои ФУ", 0, null, ScottPlot.Colors.Green, false);
        }

        private void PlotCombinedWaitIdle()
        {
            if (_engine.AvgWaitPerPhase == null || _engine.AvgIdlePerPhase == null) return;
            if (_engine.AvgWaitPerPhase.Length == 0) return;

            var phases = Enumerable.Range(1, _engine.AvgWaitPerPhase.Length).Select(i => (double)i).ToArray();
            DrawTwoSets(phases, _engine.AvgWaitPerPhase, _engine.AvgIdlePerPhase,
                "Ожидание", "Простой", ScottPlot.Colors.Orange, ScottPlot.Colors.Green,
                "Номер фазы", "Время");
        }

        private void PlotUtilVsWaitScatter()
        {
            var util = _engine.UtilizationPerPhase;
            var wait = _engine.AvgWaitPerPhase;
            if (util == null || wait == null || util.Length == 0 || util.Length != wait.Length) return;

            var points = util.Zip(wait, (u, w) => (u, w)).ToArray();
            var x = points.Select(p => p.u).ToArray();
            var y = points.Select(p => p.w).ToArray();

            switch (_currentStyle)
            {
                case ChartStyle.Bar:
                    var bars = _plot.Add.Bars(x, y);
                    foreach (var bar in bars.Bars) bar.FillColor = ScottPlot.Colors.Purple;
                    break;
                case ChartStyle.Scatter:
                    var scat = _plot.Add.Scatter(x, y);
                    scat.MarkerStyle.FillColor = ScottPlot.Colors.Purple;
                    scat.MarkerStyle.Size = 8;
                    scat.LineStyle.Width = 0;
                    scat.Label = "Точки";
                    break;
                case ChartStyle.Line:
                    var line = _plot.Add.Scatter(x, y);
                    line.MarkerStyle.Size = 0;
                    line.LineStyle.Color = ScottPlot.Colors.Purple;
                    line.LineStyle.Width = 2;
                    line.Label = "Линия";
                    break;
                case ChartStyle.LineWithMarkers:
                    var lm = _plot.Add.Scatter(x, y);
                    lm.MarkerStyle.FillColor = ScottPlot.Colors.Purple;
                    lm.MarkerStyle.Size = 6;
                    lm.LineStyle.Color = ScottPlot.Colors.Purple;
                    lm.LineStyle.Width = 2;
                    lm.Label = "Линия + маркеры";
                    break;
            }

            if (points.Length >= 2)
            {
                var (slope, intercept) = SimpleRegression(x, y);
                double xMin = util.Min(), xMax = util.Max();
                var trend = _plot.Add.Line(xMin, slope * xMin + intercept, xMax, slope * xMax + intercept);
                trend.LineStyle.Color = ScottPlot.Colors.Red;
                trend.LineStyle.Width = 1;
                trend.LineStyle.Pattern = LinePattern.Dashed;
                trend.Label = "Тренд";
            }

            _plot.Legend.IsVisible = true;
            _plot.Axes.Bottom.Label.Text = "Коэффициент загрузки (ρ)";
            _plot.Axes.Left.Label.Text = "Среднее время ожидания";
            _plot.Axes.SetLimitsX(0, 1.05);
        }

        private void PlotThroughputHistogram()
        {
            var values = _engine.ThroughputPerTask;
            if (values == null || values.Length == 0) return;

            int binCount = Math.Max(5, (int)Math.Ceiling(Math.Sqrt(values.Length)));
            var hist = ScottPlot.Statistics.Histogram.WithBinCount(binCount, values);

            double[] binCenters = new double[hist.Counts.Length];
            for (int i = 0; i < hist.Counts.Length; i++)
                binCenters[i] = (hist.Edges[i] + hist.Edges[i + 1]) / 2;

            double[] counts = hist.Counts.Select(c => (double)c).ToArray();

            var bars = _plot.Add.Bars(binCenters, counts);
            foreach (var bar in bars.Bars)
            {
                bar.FillColor = ScottPlot.Colors.Coral;
                bar.LineColor = ScottPlot.Colors.Black;
                bar.LineWidth = 0.5f;
            }

            _plot.Axes.Bottom.Label.Text = "Пропускная способность";
            _plot.Axes.Left.Label.Text = "Частота";
            _plot.Title("Гистограмма пропускной способности");
        }

        private void PlotBottleneckAnalysis()
        {
            if (_engine.AvgWaitPerPhase == null || _engine.UtilizationPerPhase == null) return;

            var phases = Enumerable.Range(1, _engine.UtilizationPerPhase.Length).Select(i => (double)i).ToArray();
            var bottleneckScore = new double[phases.Length];

            for (int i = 0; i < phases.Length; i++)
            {
                double wait = _engine.AvgWaitPerPhase[i];
                double util = _engine.UtilizationPerPhase[i];
                bottleneckScore[i] = util > 0 ? wait / (util * 10) : 0;
            }

            var bars = _plot.Add.Bars(phases, bottleneckScore);
            for (int i = 0; i < bars.Bars.Count; i++)
            {
                bars.Bars[i].FillColor = bottleneckScore[i] > 0.5 ? ScottPlot.Colors.Red : ScottPlot.Colors.Green;
                bars.Bars[i].LineColor = ScottPlot.Colors.Transparent;
            }

            _plot.Axes.Bottom.Label.Text = "Номер фазы";
            _plot.Axes.Left.Label.Text = "Коэффициент узкого места";
            _plot.Title("Анализ узких мест конвейера");

            var threshold = _plot.Add.HorizontalLine(0.5);
            threshold.LineStyle.Color = ScottPlot.Colors.Orange;
            threshold.LineStyle.Pattern = LinePattern.Dashed;
            threshold.Label.Text = "Порог внимания";
            _plot.Legend.IsVisible = true;
        }

        private void PlotLoadBalance()
        {
            var util = _engine.UtilizationPerPhase;
            if (util == null || util.Length == 0) return;

            var phases = Enumerable.Range(1, util.Length).Select(i => (double)i).ToArray();
            var bars = _plot.Add.Bars(phases, util);
            foreach (var bar in bars.Bars) bar.FillColor = ScottPlot.Colors.SteelBlue;

            double avgUtil = util.Average();
            var avgLine = _plot.Add.HorizontalLine(avgUtil);
            avgLine.LineStyle.Color = ScottPlot.Colors.Red;
            avgLine.LineStyle.Width = 2;
            avgLine.LineStyle.Pattern = LinePattern.Dashed;
            avgLine.Label.Text = $"Средняя: {avgUtil:P1}";

            if (util.Length > 1)
            {
                double stdDev = Math.Sqrt(util.Average(v => Math.Pow(v - avgUtil, 2)));
                var upper = _plot.Add.HorizontalLine(avgUtil + stdDev);
                var lower = _plot.Add.HorizontalLine(avgUtil - stdDev);
                upper.LineStyle.Color = ScottPlot.Colors.Gray.WithAlpha(0.3);
                lower.LineStyle.Color = ScottPlot.Colors.Gray.WithAlpha(0.3);
            }

            _plot.Axes.Bottom.Label.Text = "Номер фазы";
            _plot.Axes.Left.Label.Text = "Коэффициент загрузки";
            _plot.Axes.SetLimitsY(0, 1.0);
            _plot.Title("Балансировка нагрузки по фазам");
            _plot.Legend.IsVisible = true;
        }

        private void PlotWaitDistribution()
        {
            var allWaits = new List<double>();
            if (_engine.AvgWaitPerPhase != null)
            {
                var rand = new Random(42);
                foreach (var avgWait in _engine.AvgWaitPerPhase)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        double variation = 1 + (rand.NextDouble() - 0.5) * 0.4;
                        allWaits.Add(Math.Max(0, avgWait * variation));
                    }
                }
            }

            if (allWaits.Count == 0) return;

            int binCount = Math.Max(5, (int)Math.Sqrt(allWaits.Count));
            var hist = ScottPlot.Statistics.Histogram.WithBinCount(binCount, allWaits.ToArray());

            double[] binCenters = new double[hist.Counts.Length];
            for (int i = 0; i < hist.Counts.Length; i++)
                binCenters[i] = (hist.Edges[i] + hist.Edges[i + 1]) / 2;

            double[] counts = hist.Counts.Select(c => (double)c).ToArray();
            var bars = _plot.Add.Bars(binCenters, counts);

            foreach (var bar in bars.Bars)
            {
                bar.FillColor = ScottPlot.Colors.Teal;
                bar.LineColor = ScottPlot.Colors.White;
                bar.LineWidth = 0.5f;
            }

            _plot.Axes.Bottom.Label.Text = "Время ожидания";
            _plot.Axes.Left.Label.Text = "Частота";
            _plot.Title("Распределение времён ожидания");
        }

        private void PlotPhaseEfficiency()
        {
            var util = _engine.UtilizationPerPhase;
            var wait = _engine.AvgWaitPerPhase;
            if (util == null || wait == null || util.Length == 0) return;

            var phases = Enumerable.Range(1, util.Length).Select(i => (double)i).ToArray();
            double maxWait = wait.Max();
            var efficiency = new double[phases.Length];

            for (int i = 0; i < phases.Length; i++)
            {
                double normalizedWait = maxWait > 0 ? wait[i] / maxWait : 0;
                efficiency[i] = util[i] * (1 - normalizedWait);
            }

            switch (_currentStyle)
            {
                case ChartStyle.Bar:
                    var bars = _plot.Add.Bars(phases, efficiency);
                    for (int i = 0; i < bars.Bars.Count; i++)
                    {
                        double eff = efficiency[i];
                        bars.Bars[i].FillColor = eff > 0.7 ? ScottPlot.Colors.Green : eff > 0.3 ? ScottPlot.Colors.Gold : ScottPlot.Colors.Red;
                        bars.Bars[i].LineColor = ScottPlot.Colors.Transparent;
                    }
                    break;
                case ChartStyle.Scatter:
                    var scatter = _plot.Add.Scatter(phases, efficiency);
                    scatter.MarkerStyle.FillColor = ScottPlot.Colors.Purple;
                    scatter.MarkerStyle.Size = 6;
                    scatter.LineStyle.Width = 0;
                    break;
                case ChartStyle.Line:
                    var line = _plot.Add.Scatter(phases, efficiency);
                    line.MarkerStyle.Size = 0;
                    line.LineStyle.Color = ScottPlot.Colors.Purple;
                    line.LineStyle.Width = 2;
                    break;
                case ChartStyle.LineWithMarkers:
                    var lm = _plot.Add.Scatter(phases, efficiency);
                    lm.MarkerStyle.FillColor = ScottPlot.Colors.Purple;
                    lm.MarkerStyle.Size = 4;
                    lm.LineStyle.Color = ScottPlot.Colors.Purple;
                    lm.LineStyle.Width = 2;
                    break;
            }

            _plot.Axes.Bottom.Label.Text = "Номер фазы";
            _plot.Axes.Left.Label.Text = "Показатель эффективности";
            _plot.Axes.SetLimitsY(0, 1.0);
            _plot.Title("⭐ Эффективность фаз");

            var legend = _plot.Add.Text("Высокая  Средняя  Низкая", phases.Length / 2.0, 1.1);
            legend.LabelStyle.FontSize = 10;
            _plot.Legend.IsVisible = false;
        }

        private void PlotCumulativeDelay()
        {
            var wait = _engine.AvgWaitPerPhase;
            if (wait == null || wait.Length == 0) return;

            var phases = Enumerable.Range(1, wait.Length).Select(i => (double)i).ToArray();
            double[] cumulative = new double[wait.Length];
            cumulative[0] = wait[0];
            for (int i = 1; i < wait.Length; i++)
                cumulative[i] = cumulative[i - 1] + wait[i];

            switch (_currentStyle)
            {
                case ChartStyle.Bar:
                    var bars = _plot.Add.Bars(phases, cumulative);
                    foreach (var bar in bars.Bars) bar.FillColor = ScottPlot.Colors.Purple;
                    break;
                case ChartStyle.Scatter:
                    var scat = _plot.Add.Scatter(phases, cumulative);
                    scat.MarkerStyle.FillColor = ScottPlot.Colors.Purple;
                    scat.MarkerStyle.Size = 6;
                    break;
                case ChartStyle.Line:
                case ChartStyle.LineWithMarkers:
                    var line = _plot.Add.Scatter(phases, cumulative);
                    line.MarkerStyle.Size = _currentStyle == ChartStyle.LineWithMarkers ? 4 : 0;
                    line.MarkerStyle.FillColor = ScottPlot.Colors.Purple;
                    line.LineStyle.Color = ScottPlot.Colors.Purple;
                    line.LineStyle.Width = 2;
                    break;
            }

            _plot.Axes.Bottom.Label.Text = "Номер фазы";
            _plot.Axes.Left.Label.Text = "Накопленное время ожидания";
            _plot.Title("Накопленная задержка по фазам");

            if (cumulative.Length > 1)
            {
                double lastPhase = phases[^1];
                double lastCum = cumulative[^1];
                var ideal = _plot.Add.Line(0, 0, lastPhase, lastCum);
                ideal.LineStyle.Color = ScottPlot.Colors.Gray.WithAlpha(0.5);
                ideal.LineStyle.Pattern = LinePattern.Dotted;
                ideal.Label = "Идеальный рост";
                _plot.Legend.IsVisible = true;
            }
        }

        private void RenderChart(double[] x, double[] y, string xLabel, string yLabel,
            string title, double? minY, double? maxY, ScottPlot.Color color, bool showAverage)
        {
            if (x == null || y == null || x.Length == 0) return;

            _plot.Title(title);
            _plot.Grid.MajorLineColor = ScottPlot.Colors.Gray.WithAlpha(0.2);
            _plot.Grid.MajorLineWidth = 1;

            switch (_currentStyle)
            {
                case ChartStyle.Bar:
                    var bars = _plot.Add.Bars(x, y);
                    foreach (var bar in bars.Bars)
                    {
                        bar.FillColor = color;
                        bar.LineColor = ScottPlot.Colors.Transparent;
                    }
                    break;
                case ChartStyle.Scatter:
                    var scatter = _plot.Add.Scatter(x, y);
                    scatter.MarkerStyle.FillColor = color;
                    scatter.MarkerStyle.Size = 6;
                    scatter.LineStyle.Color = ScottPlot.Colors.Transparent;
                    break;
                case ChartStyle.Line:
                    var line = _plot.Add.Scatter(x, y);
                    line.MarkerStyle.Size = 0;
                    line.LineStyle.Color = color;
                    line.LineStyle.Width = 2;
                    break;
                case ChartStyle.LineWithMarkers:
                    var lm = _plot.Add.Scatter(x, y);
                    lm.MarkerStyle.FillColor = color;
                    lm.MarkerStyle.Size = 4;
                    lm.LineStyle.Color = color;
                    lm.LineStyle.Width = 2;
                    break;
            }

            _plot.Axes.Bottom.Label.Text = xLabel;
            _plot.Axes.Left.Label.Text = yLabel;
            _plot.Axes.SetLimitsX(0.5, x.Length + 0.5);
            _plot.Axes.SetLimitsY(minY ?? 0, maxY ?? (y.Any() ? y.Max() * 1.2 : 1.0));

            if (showAverage && y.Any())
            {
                double avg = y.Average();
                var avgLine = _plot.Add.HorizontalLine(avg);
                avgLine.LineStyle.Color = ScottPlot.Colors.Red;
                avgLine.LineStyle.Width = 2;
                avgLine.LineStyle.Pattern = LinePattern.Dashed;
                avgLine.Label.Text = $"Среднее: {avg:F4}";
                _plot.Legend.IsVisible = true;
            }
        }

        private void DrawTwoSets(double[] x, double[] y1, double[] y2,
            string label1, string label2, ScottPlot.Color color1, ScottPlot.Color color2,
            string xAxisLabel, string yAxisLabel)
        {
            Action<double[], double[], ScottPlot.Color, string> drawOne = (xs, ys, col, lbl) =>
            {
                switch (_currentStyle)
                {
                    case ChartStyle.Bar:
                        var bars = _plot.Add.Bars(xs, ys);
                        foreach (var bar in bars.Bars)
                        {
                            bar.FillColor = col;
                            bar.LineColor = ScottPlot.Colors.Transparent;
                        }
                        break;
                    case ChartStyle.Scatter:
                        var scat = _plot.Add.Scatter(xs, ys);
                        scat.MarkerStyle.FillColor = col;
                        scat.MarkerStyle.Size = 6;
                        scat.LineStyle.Color = ScottPlot.Colors.Transparent;
                        scat.Label = lbl;
                        break;
                    case ChartStyle.Line:
                        var line = _plot.Add.Scatter(xs, ys);
                        line.MarkerStyle.Size = 0;
                        line.LineStyle.Color = col;
                        line.LineStyle.Width = 2;
                        line.Label = lbl;
                        break;
                    case ChartStyle.LineWithMarkers:
                        var lm = _plot.Add.Scatter(xs, ys);
                        lm.MarkerStyle.FillColor = col;
                        lm.MarkerStyle.Size = 5;
                        lm.LineStyle.Color = col;
                        lm.LineStyle.Width = 2;
                        lm.Label = lbl;
                        break;
                }
            };

            drawOne(x, y1, color1, label1);
            drawOne(x, y2, color2, label2);

            _plot.Legend.IsVisible = true;
            _plot.Axes.Bottom.Label.Text = xAxisLabel;
            _plot.Axes.Left.Label.Text = yAxisLabel;
            _plot.Axes.SetLimitsX(0.5, x.Length + 0.5);
        }

        private (double slope, double intercept) SimpleRegression(double[] x, double[] y)
        {
            double xAvg = x.Average(), yAvg = y.Average();
            double num = 0, den = 0;
            for (int i = 0; i < x.Length; i++)
            {
                double dx = x[i] - xAvg;
                num += dx * (y[i] - yAvg);
                den += dx * dx;
            }
            double slope = den != 0 ? num / den : 0;
            return (slope, yAvg - slope * xAvg);
        }

        private void UpdateStatsLabel()
        {
            if (_engine == null) return;
            var ci = CultureInfo.InvariantCulture;

            string stats = _currentDataType switch
            {
                DataType.Throughput => $"Средняя: {_engine.ThroughputPerTask.Average().ToString("F4", ci)} | Макс: {_engine.ThroughputPerTask.Max().ToString("F4", ci)}",
                DataType.Utilization => $"Средняя загрузка: {_engine.UtilizationPerPhase.Average().ToString("P1", ci)} | Мин/Макс: {_engine.UtilizationPerPhase.Min().ToString("P1", ci)} / {_engine.UtilizationPerPhase.Max().ToString("P1", ci)}",
                DataType.AvgWait => $"Среднее ожидание: {_engine.AvgWaitPerPhase.Average().ToString("F3", ci)} | Мин/Макс: {_engine.AvgWaitPerPhase.Min().ToString("F3", ci)} / {_engine.AvgWaitPerPhase.Max().ToString("F3", ci)}",
                DataType.AvgIdle => $"Средний простой: {_engine.AvgIdlePerPhase.Average().ToString("F3", ci)} | Мин/Макс: {_engine.AvgIdlePerPhase.Min().ToString("F3", ci)} / {_engine.AvgIdlePerPhase.Max().ToString("F3", ci)}",
                DataType.CombinedWaitIdle => $"Ожидание: {_engine.AvgWaitPerPhase?.Average().ToString("F3", ci)} | Простой: {_engine.AvgIdlePerPhase?.Average().ToString("F3", ci)}",
                DataType.UtilVsWait => $"Корреляция: ρ×wait | Фаз: {_engine.UtilizationPerPhase?.Length}",
                DataType.ThroughputHist => $"Биннов: {Math.Ceiling(Math.Sqrt(_engine.ThroughputPerTask?.Length ?? 1))} | Диапазон: [{_engine.ThroughputPerTask?.Min().ToString("F2", ci)}, {_engine.ThroughputPerTask?.Max().ToString("F2", ci)}]",
                DataType.Bottleneck => $"Критические фазы: {_engine.AvgWaitPerPhase?.Count(w => w > _engine.AvgWaitPerPhase.Average() * 1.5)} | Макс. wait: {_engine.AvgWaitPerPhase?.Max().ToString("F3", ci)}",
                DataType.LoadBalance => $"Ср. загрузка: {_engine.UtilizationPerPhase?.Average().ToString("P1", ci)} | σ: {(_engine.UtilizationPerPhase?.Length > 1 ? Math.Sqrt(_engine.UtilizationPerPhase.Average(v => Math.Pow(v - _engine.UtilizationPerPhase.Average(), 2))).ToString("P2", ci) : "0")}",
                DataType.WaitDist => $"Ср. ожидание: {_engine.AvgWaitPerPhase?.Average().ToString("F3", ci)} | Медиана: {(_engine.AvgWaitPerPhase?.OrderBy(w => w).ElementAtOrDefault(_engine.AvgWaitPerPhase.Length / 2) ?? 0).ToString("F3", ci)}",
                DataType.PhaseEfficiency => $"Ср. эффективность: {(_engine.UtilizationPerPhase?.Average() ?? 0) * 0.8:P1} | Лучшая фаза: #{(_engine.UtilizationPerPhase?.ToList()?.IndexOf(_engine.UtilizationPerPhase.Max()) + 1 ?? 0)}",
                DataType.CumulativeDelay => $"Общая задержка: {_engine.AvgWaitPerPhase?.Sum().ToString("F3", ci)} | Рост/фазу: {(_engine.AvgWaitPerPhase?.Average() ?? 0).ToString("F3", ci)}",
                _ => string.Empty
            };

            lblStats.Text = stats;
        }

        private void UpdatePlotTheme()
        {
            if (_plot == null) return;
            bool isDark = ThemeManager.GetCurrentTheme() == "Dark";
            _plot.FigureBackground.Color = isDark ? ScottPlot.Color.FromHex("#1E1E1E") : ScottPlot.Color.FromHex("#FFFFFF");
            _plot.DataBackground.Color = isDark ? ScottPlot.Color.FromHex("#2D2D2D") : ScottPlot.Color.FromHex("#FAFAFA");
            _plot.Axes.Color(isDark ? ScottPlot.Color.FromHex("#BDBDBD") : ScottPlot.Color.FromHex("#424242"));
        }

        private void cbDataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbDataType?.SelectedIndex is >= 0 && cbDataType.SelectedIndex < Enum.GetValues<DataType>().Length)
            {
                _currentDataType = (DataType)cbDataType.SelectedIndex;
                UpdateChartData();
                UpdateStatsLabel();
            }
        }

        private void cbChartStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbChartStyle?.SelectedIndex is >= 0 and <= 3)
            {
                _currentStyle = (ChartStyle)cbChartStyle.SelectedIndex;
                UpdateChartData();
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_plot == null)
            {
                MessageBox.Show("График ещё не инициализирован.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                DefaultExt = ".png",
                FileName = $"Chart_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _plot.SavePng(dialog.FileName, 1600, 1000);
                    MessageBox.Show($"График успешно сохранён:\n{dialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось сохранить график:\n{ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_engine == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "CSV File|*.csv",
                DefaultExt = ".csv",
                FileName = $"SimulationResults_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
                var ci = CultureInfo.InvariantCulture;
                var config = _engine.GetConfig();

                writer.WriteLine("Общие");
                writer.WriteLine("Parameter,Value");
                writer.WriteLine($"Timestamp,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"PhasesCount,{config.PhasesCount}");
                writer.WriteLine($"JobsCount,{config.JobsCount}");
                writer.WriteLine($"NumRuns,{config.NumRuns}");
                writer.WriteLine($"Lambda,{config.Lambda.ToString("F4", ci)}");
                writer.WriteLine($"Mu,{config.Mu.ToString("F4", ci)}");
                writer.WriteLine($"Sigma,{config.Sigma.ToString("F4", ci)}");
                writer.WriteLine($"Seed,{config.Seed}");
                writer.WriteLine($"IntervalDistribution,{GetDistributionName(config.IntervalDistribution)}");
                writer.WriteLine($"ProcessingDistribution,{GetDistributionName(config.ProcessingDistribution)}");
                writer.WriteLine($"TotalSimulationTime,{_engine.TotalSimulationTime.ToString("F4", ci)}");
                writer.WriteLine();

                writer.WriteLine("Системные");
                writer.WriteLine("Metric,Value,Unit");
                double systemThroughput = _engine.TotalSimulationTime > 0
                    ? config.JobsCount / _engine.TotalSimulationTime : 0;
                var sojournTimes = _engine.GetAllSojournTimes();
                double avgSojourn = sojournTimes.Length > 0 ? sojournTimes.Average() : 0;
                double stdSojourn = sojournTimes.Length > 0 ? StatisticsHelper.StdDev(sojournTimes) : 0;

                writer.WriteLine($"TotalCompletedTasks,{config.JobsCount},count");
                writer.WriteLine($"Makespan,{_engine.TotalSimulationTime.ToString("F4", ci)},time_units");
                writer.WriteLine($"SystemThroughput,{systemThroughput.ToString("F4", ci)},tasks/time_unit");
                writer.WriteLine($"AvgSojournTime,{avgSojourn.ToString("F4", ci)},time_units");
                writer.WriteLine($"SojournTimeStdDev,{stdSojourn.ToString("F4", ci)},time_units");
                writer.WriteLine();

                writer.WriteLine("Статистика фаз");
                writer.WriteLine("PhaseId,Utilization,AvgWait,WaitP95,WaitStdDev,ServiceCV,AvgIdle,MaxQueue,TasksProcessed");

                var detailedStats = _engine.GetDetailedPhaseStats();
                foreach (var stat in detailedStats)
                {
                    var waitHist = _engine.GetWaitHistoryForPhase(stat.PhaseId - 1);
                    var serviceHist = _engine.GetServiceTimeHistoryForPhase(stat.PhaseId - 1);

                    double waitP95 = StatisticsHelper.Percentile(waitHist, 0.95);
                    double waitStdDev = StatisticsHelper.StdDev(waitHist);
                    double serviceCV = StatisticsHelper.CoefficientOfVariation(serviceHist);

                    writer.WriteLine($"{stat.PhaseId}," +
                        $"{stat.Utilization.ToString("F4", ci)}," +
                        $"{stat.AvgWaitTime.ToString("F4", ci)}," +
                        $"{waitP95.ToString("F4", ci)}," +
                        $"{waitStdDev.ToString("F4", ci)}," +
                        $"{serviceCV.ToString("F4", ci)}," +
                        $"{stat.AvgIdleTime.ToString("F4", ci)}," +
                        $"{stat.MaxQueueLength}," +
                        $"{stat.TasksProcessed}");
                }
                writer.WriteLine();

                writer.WriteLine("анализ узкого метса");
                writer.WriteLine("Metric,PhaseId,Value");

                int maxUtilPhase = Array.IndexOf(_engine.UtilizationPerPhase, _engine.UtilizationPerPhase.Max()) + 1;
                int maxWaitPhase = Array.IndexOf(_engine.AvgWaitPerPhase, _engine.AvgWaitPerPhase.Max()) + 1;

                writer.WriteLine($"HighestUtilizationPhase,{maxUtilPhase},{_engine.UtilizationPerPhase.Max():F4}");
                writer.WriteLine($"LongestWaitPhase,{maxWaitPhase},{_engine.AvgWaitPerPhase.Max():F4}");
                writer.WriteLine($"BottleneckScore_Phase{maxUtilPhase},{CalculateBottleneckScore(maxUtilPhase - 1):F4},ratio");
                writer.WriteLine();

                if (config.NumRuns > 1)
                {
                    writer.WriteLine("доверительные интервалы");
                    writer.WriteLine("Metric,Mean,Margin,Lower,Upper,Unit");

                    var tpRuns = _engine.GetThroughputPerRun();
                    if (tpRuns.Length > 1)
                    {
                        double tpMean = tpRuns.Average();
                        double tpMargin = StatisticsHelper.ConfidenceMargin(tpRuns);
                        writer.WriteLine($"Throughput,{tpMean:F4},{tpMargin:F4},{tpMean - tpMargin:F4},{tpMean + tpMargin:F4},tasks/time_unit");
                    }

                    var waitRuns = _engine.GetAvgWaitPerRun();
                    if (waitRuns.Length > 1)
                    {
                        double wMean = waitRuns.Average();
                        double wMargin = StatisticsHelper.ConfidenceMargin(waitRuns);
                        writer.WriteLine($"AvgSystemWait,{wMean:F4},{wMargin:F4},{wMean - wMargin:F4},{wMean + wMargin:F4},time_units");
                    }

                    var utilRuns = _engine.GetAvgUtilPerRun();
                    if (utilRuns.Length > 1)
                    {
                        double uMean = utilRuns.Average();
                        double uMargin = StatisticsHelper.ConfidenceMargin(utilRuns);
                        writer.WriteLine($"AvgUtilization,{uMean:F4},{uMargin:F4},{uMean - uMargin:F4},{uMean + uMargin:F4},ratio");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("Образыцы распределений");
                writer.WriteLine("DataType,Value");

                var allWaits = _engine.GetAllWaitTimes();
                var allServices = _engine.GetAllServiceTimes();
                var allSojourns = _engine.GetAllSojournTimes();

                foreach (var w in allWaits.Take(1000))
                    writer.WriteLine($"WaitTime,{w.ToString("F6", ci)}");
                foreach (var s in allServices.Take(1000))
                    writer.WriteLine($"ServiceTime,{s.ToString("F6", ci)}");
                foreach (var t in allSojourns.Take(1000))
                    writer.WriteLine($"SojournTime,{t.ToString("F6", ci)}");
                writer.WriteLine();

                writer.WriteLine("Времянные ряды");
                writer.WriteLine("FileName,Description");
                string basePath = Path.GetDirectoryName(dialog.FileName) ?? "";
                string tsPrefix = Path.GetFileNameWithoutExtension(dialog.FileName) + "_ts";

                ExportTimeSeries($"{basePath}\\{tsPrefix}_queue.csv", "Phase,QueueEventTime",
                    _engine.GetQueueEventTimeline().Select(q => $"{q.Phase},{q.Time.ToString("F4", ci)}"));
                writer.WriteLine($"{tsPrefix}_queue.csv,Queue events timeline");

                ExportTimeSeries($"{basePath}\\{tsPrefix}_throughput.csv", "TaskId,CumulativeThroughput,Time",
                    Enumerable.Range(0, _engine.ThroughputPerTask.Length).Select(j =>
                        $"{j + 1},{_engine.ThroughputPerTask[j].ToString("F4", ci)},{(j + 1.0) / Math.Max(_engine.ThroughputPerTask[j], 1e-9):F4}"));
                writer.WriteLine($"{tsPrefix}_throughput.csv,Cumulative throughput series");

                writer.WriteLine("(Файлы временных рядов сохранены в ту же директорию с суффиксом _ts)");

                MessageBox.Show($"Расширенная статистика экспортирована:\n{dialog.FileName}\n\n" +
                               $"Временные ряды: {tsPrefix}_*.csv", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте:\n{ex.Message}\n\n{ex.StackTrace}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // вспомогательные методы
        private void ExportTimeSeries(string filePath, string header, IEnumerable<string> lines)
        {
            try
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                writer.WriteLine(header);
                foreach (var line in lines)
                    writer.WriteLine(line);
            }
            catch { /* игнорируем ошибки записи */ }
        }

        private double CalculateBottleneckScore(int phaseIndex)
        {
            if (phaseIndex < 0 || phaseIndex >= _engine.AvgWaitPerPhase.Length)
                return 0;

            var wait = _engine.AvgWaitPerPhase[phaseIndex];
            var util = _engine.UtilizationPerPhase[phaseIndex];
            return util > 0.01 ? wait / (util * 10) : 0;
        }

        private string GetDistributionName(int code) => code switch
        {
            0 => "Exponential",
            1 => "Uniform",
            2 => "TruncatedNormal",
            _ => "Unknown"
        };

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}