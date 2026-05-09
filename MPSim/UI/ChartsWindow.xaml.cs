using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MPSim.Services;
using ScottPlot;
using ScottPlot.Plottables;

namespace MPSim.UI
{
    public partial class ChartsWindow : Window
    {
        private Plot _plot;
        //private Plot _plot = null;
        private readonly Random _random = new Random(42); // временно

        public ChartsWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => InitializePlot(); // инит только после загрузки
        }

        private void InitializePlot()
        {
            if (MainPlot?.Plot == null)
            {
                MessageBox.Show(
                    "ERROR: График не инициализирован.\nПроверьте:\n" +
                    "1. В XAML: xmlns:ScottPlot=\"clr-namespace:ScottPlot.Wpf;assembly=ScottPlot.Wpf\"\n" +
                    "2. В XAML: <ScottPlot:WpfPlot x:Name=\"MainPlot\"/>\n" +
                    "3. Установлен NuGet-пакет: ScottPlot.WPF",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _plot = MainPlot.Plot;

            _plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
            _plot.DataBackground.Color = ScottPlot.Color.FromHex("#FAFAFA");

            UpdatePlotTheme();
            UpdateChartData();
            MainPlot.Refresh();
        }

        private void UpdateChartData()
        {
            if (_plot == null) return;
            _plot.Clear();


            var phases = Enumerable.Range(1, 10).Select(x => (double)x).ToArray();
            var values = phases.Select(p => 0.3 + _random.NextDouble() * 0.4).ToArray();

            var bar = _plot.Add.Bars(phases, values);
            foreach (var b in bar.Bars)
            {
                b.FillColor = ScottPlot.Color.FromHex("#2196F3");
            }

            _plot.Axes.Bottom.Label.Text = "Номер фазы";
            _plot.Axes.Left.Label.Text = "Коэффициент загрузки (ρ)";
            _plot.Axes.SetLimitsX(0.5, 10.5);
            _plot.Axes.SetLimitsY(0, 1.0);
        }

        private void cbChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChartData();
            MainPlot?.Refresh();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_plot == null) return;
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                DefaultExt = ".png",
                FileName = $"Chart_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                _plot.SavePng(dialog.FileName, 1200, 800);
                MessageBox.Show("График сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
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

                writer.WriteLine("Phase,LoadFactor,AvgWaitTime,AvgIdleTime,Throughput");

                var phases = Enumerable.Range(1, 10);
                foreach (var p in phases)
                {
                    double load = 0.3 + _random.NextDouble() * 0.4;
                    double wait = _random.NextDouble() * 2.0;
                    double idle = _random.NextDouble() * 1.5;
                    double throughput = 0.8 + p * 0.02;

                    writer.WriteLine($"{p},{load:F4},{wait:F4},{idle:F4},{throughput:F4}");
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
            var isDark = ThemeManager.GetCurrentTheme() == "Dark";

            _plot.FigureBackground.Color = isDark
                ? ScottPlot.Color.FromHex("#1E1E1E")
                : ScottPlot.Color.FromHex("#FFFFFF");

            _plot.DataBackground.Color = isDark
                ? ScottPlot.Color.FromHex("#2D2D2D")
                : ScottPlot.Color.FromHex("#FAFAFA");

            _plot.Axes.Color(isDark
                ? ScottPlot.Color.FromHex("#BDBDBD")
                : ScottPlot.Color.FromHex("#424242"));
        }
    }
}