using System.Windows;
using MPSim.UI;
using MPSim.Models;
using MPSim.Services;

namespace MPSim
{
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty IsDarkThemeProperty =
        DependencyProperty.Register("IsDarkTheme", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool IsDarkTheme
        {
            get => (bool)GetValue(IsDarkThemeProperty);
            set => SetValue(IsDarkThemeProperty, value);
        }

        private SimulationConfig _config;

        public MainWindow()
        {
            InitializeComponent();
            _config = new SimulationConfig();
            ThemeManager.ApplyTheme("Light");
            IsDarkTheme = false;
        }

        private void btnTheme_Click(object sender, RoutedEventArgs e)
        {
            var newTheme = ThemeManager.ToggleTheme();
            IsDarkTheme = newTheme == "Dark";
            txtStatus.Text = $"Тема: {newTheme}";
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new ConfigWindow(_config) { Owner = this };
            if (settingsWindow.ShowDialog() == true)
            {
                _config = settingsWindow.ResultConfig;
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            txtStatus.Text = "Симуляция запущена...";

            SimulationArea.Visibility = Visibility.Visible;
            PlaceholderArea.Visibility = Visibility.Collapsed;

            LoadTestData();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            txtStatus.Text = "Симуляция остановлена";
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            SimulationArea.Visibility = Visibility.Collapsed;
            PlaceholderArea.Visibility = Visibility.Visible;
            txtStatus.Text = "Готов к работе";
            dgTasks.ItemsSource = null;
            dgPhases.ItemsSource = null;
        }

        private void btnResults_Click(object sender, RoutedEventArgs e)
        {
            var chartsWindow = new ChartsWindow { Owner = this };
            chartsWindow.ShowDialog();
        }

        private void LoadTestData()
        {
            var tasks = new[]
            {
                new { Id = 1, ArrivalTime = 1.2, WaitTime = 0.0, Status = "Завершено" },
                new { Id = 2, ArrivalTime = 2.5, WaitTime = 0.3, Status = "В обработке" },
                new { Id = 3, ArrivalTime = 3.8, WaitTime = 0.0, Status = "Ожидает" },
            };
            dgTasks.ItemsSource = tasks;

            var phases = new[]
            {
                new { Id = 1, Utilization = 0.42, IdlePercent = 0.15, ProcessedCount = 85 },
                new { Id = 2, Utilization = 0.38, IdlePercent = 0.22, ProcessedCount = 78 },
                new { Id = 3, Utilization = 0.45, IdlePercent = 0.12, ProcessedCount = 91 },
            };
            dgPhases.ItemsSource = phases;
        }
    }
}