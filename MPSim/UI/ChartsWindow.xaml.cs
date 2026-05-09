using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MPSim.Core;
using MPSim.Services;
using ScottPlot;

namespace MPSim.UI
{
    public partial class ChartsWindow : Window
    {
        private Plot _plot;
        private readonly SimulationEngine _engine;
        private int _currentChartType;

        public ChartsWindow(SimulationEngine engine)
        {
            InitializeComponent();
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _currentChartType = 0;

            Loaded += (s, e) => InitializePlot(); // инит только после загрузки
        }

        private void UpdateChartData()
        {
            if (_plot == null || _engine?.ThroughputPerTask == null) return;
            _plot.Clear();

            switch (_currentChartType)
            {
                case 0: PlotThroughput(); break;
                case 1: PlotUtilization(); break;
                case 2: PlotAvgWait(); break;
                case 3: PlotAvgIdle(); break;
            }
            MainPlot?.Refresh();
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
            MainPlot.Refresh();
        }

        private void PlotThroughput()
        {
            var x = Enumerable.Range(1, _engine.ThroughputPerTask.Length)
                .Select(i => (double)i).ToArray();
            var y = _engine.ThroughputPerTask;

            var scatter = _plot.Add.Scatter(x, y);
            scatter.MarkerStyle.FillColor = ScottPlot.Colors.Blue;
            scatter.MarkerStyle.Size = 3;
            scatter.LineStyle.Color = Colors.Blue;

            var avgLine = _plot.Add.Line(x.First(), y.Average(), x.Last(), y.Average());
            avgLine.Color = Colors.Red;
            avgLine.LineStyle.Width = 2;
            avgLine.LineStyle.Pattern = LinePattern.Dashed;

            _plot.Axes.Bottom.Label.Text = "Номер задания (N)";
            _plot.Axes.Left.Label.Text = "Накопленная пропускная способность";
            _plot.Axes.SetLimitsX(0, x.Length + 1);
        }

        private void PlotUtilization()
        {
            var x = Enumerable.Range(1, _engine.UtilizationPerPhase.Length)
                .Select(i => (double)i).ToArray();
            var y = _engine.UtilizationPerPhase;

            var bars = _plot.Add.Bars(x, y);
            foreach (var bar in bars.Bars)
            {
                bar.FillColor = Colors.SteelBlue;
                bar.LineColor = ScottPlot.Colors.Transparent;
                bar.LineWidth = 0;
            }

            _plot.Axes.Bottom.Label.Text = "Номер фазы";
            _plot.Axes.Left.Label.Text = "Коэффициент загрузки (ρ)";
            _plot.Axes.SetLimitsX(0.5, x.Length + 0.5);
            _plot.Axes.SetLimitsY(0, 1.0);
        }

        private void PlotAvgWait()
        {
            var x = Enumerable.Range(1, _engine.AvgWaitPerPhase.Length)
                .Select(i => (double)i).ToArray();
            var y = _engine.AvgWaitPerPhase;

            var scatter = _plot.Add.Scatter(x, y);
            scatter.MarkerStyle.FillColor = ScottPlot.Colors.Orange;
            scatter.MarkerStyle.Size = 6;
            scatter.LineStyle.Color = Colors.Transparent; // убираем линию оставляем точки

            _plot.Axes.Bottom.Label.Text = "Номер фазы";
            _plot.Axes.Left.Label.Text = "Среднее время ожидания";
            _plot.Axes.SetLimitsX(0.5, x.Length + 0.5);
        }

        private void PlotAvgIdle()
        {
            var x = Enumerable.Range(1, _engine.AvgIdlePerPhase.Length)
                .Select(i => (double)i).ToArray();
            var y = _engine.AvgIdlePerPhase;

            var scatter = _plot.Add.Scatter(x, y);
            scatter.MarkerStyle.FillColor = ScottPlot.Colors.Green;
            scatter.MarkerStyle.Size = 6;
            scatter.LineStyle.Color = Colors.Transparent;

            _plot.Axes.Bottom.Label.Text = "Номер фазы";
            _plot.Axes.Left.Label.Text = "Среднее время простоя";
            _plot.Axes.SetLimitsX(0.5, x.Length + 0.5);
        }
        //private void UpdateChartData()
        //{
        //    if (_plot == null) return;
        //    _plot.Clear();


        //    var phases = Enumerable.Range(1, 10).Select(x => (double)x).ToArray();
        //    var values = phases.Select(p => 0.3 + _random.NextDouble() * 0.4).ToArray();

        //    var bar = _plot.Add.Bars(phases, values);
        //    foreach (var b in bar.Bars)
        //    {
        //        b.FillColor = ScottPlot.Color.FromHex("#2196F3");
        //    }

        //    _plot.Axes.Bottom.Label.Text = "Номер фазы";
        //    _plot.Axes.Left.Label.Text = "Коэффициент загрузки (ρ)";
        //    _plot.Axes.SetLimitsX(0.5, 10.5);
        //    _plot.Axes.SetLimitsY(0, 1.0);
        //}

        private void cbChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbChartType?.SelectedIndex is >= 0 and <= 3)
            {
                _currentChartType = cbChartType.SelectedIndex;
                UpdateChartData();
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = $"Chart_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                _plot?.SavePng(dialog.FileName, 1200, 800);
                MessageBox.Show("График сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_engine == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "CSV File|*.csv",
                DefaultExt = ".csv",
                FileName = $"SimulationData_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);

                writer.WriteLine("Phase,LoadFactor,AvgWaitTime,AvgIdleTime");

                for (int i = 0; i < _engine.UtilizationPerPhase.Length; i++)
                {
                    writer.WriteLine($"{i + 1}," +
                        $"{_engine.UtilizationPerPhase[i]:F4}," +
                        $"{_engine.AvgWaitPerPhase[i]:F4}," +
                        $"{_engine.AvgIdlePerPhase[i]:F4}");
                }

                MessageBox.Show($"Данные успешно экспортированы:\n{dialog.FileName}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePlotTheme()
        {
            if (_plot == null) return;
            bool isDark = ThemeManager.GetCurrentTheme() == "Dark";
            var bgColor = isDark ? Color.FromHex("#1E1E1E") : Color.FromHex("#FFFFFF");
            var dataColor = isDark ? Color.FromHex("#2D2D2D") : Color.FromHex("#FAFAFA");
            var axisColor = isDark ? Color.FromHex("#BDBDBD") : Color.FromHex("#424242");

            _plot.FigureBackground.Color = bgColor;
            _plot.DataBackground.Color = dataColor;
            _plot.Axes.Color(axisColor);
        }
    }
}