using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ScottPlot;

namespace MPSim.UI
{
    public partial class ChartsWindow : Window
    {
        private Plot _plot;

        public ChartsWindow()
        {
            InitializeComponent();
            InitializePlot();
        }

        private void InitializePlot()
        {
            _plot = MainPlot.Plot;
            _plot.Style(ScottPlot.Style.Light1);
            UpdateChartData();
            MainPlot.Refresh();
        }

        private void UpdateChartData()
        {
            _plot.Clear();
            var phases = Enumerable.Range(1, 10).ToArray();
            var values = phases.Select(p => 0.3 + new Random().NextDouble() * 0.4).ToArray();

            var bar = _plot.Add.Bars(phases, values);
            bar.Color = _plot.Palette.Colors[0];

            _plot.XAxis.Label.Text = "Фаза";
            _plot.YAxis.Label.Text = "Значение";
            _plot.Axes.SetLimitsX(0, 11);
        }

        private void cbChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChartData();
            MainPlot.Refresh();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
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
    }
}