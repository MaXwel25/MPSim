using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MPSim.Core;
using MPSim.Models;
using MPSim.Services;
using MPSim.UI;


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
        private SimulationEngine? _engine;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        private ObservableCollection<TaskViewModel> _tasksCollection;
        private ObservableCollection<PhaseViewModel> _phasesCollection;


        public MainWindow()
        {
            InitializeComponent();
            _config = new SimulationConfig();
            _tasksCollection = new ObservableCollection<TaskViewModel>();
            _phasesCollection = new ObservableCollection<PhaseViewModel>();

            dgTasks.ItemsSource = _tasksCollection;
            dgPhases.ItemsSource = _phasesCollection;

            ThemeManager.ApplyTheme("Light");
            IsDarkTheme = false;
            UpdateConfigDisplay(); // для обязательно отображения внизу
            UpdateButtonsState();
        }

        private void UpdateButtonsState()
        {
            btnStart.IsEnabled = !_isRunning;
            btnStop.IsEnabled = _isRunning;
            btnSettings.IsEnabled = !_isRunning;
            btnResults.IsEnabled = _engine != null && !_isRunning;
        }

        // для обновления статистики снизу
        private void UpdateConfigDisplay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lblPhases.Text = _config.PhasesCount.ToString();
                lblJobs.Text = _config.JobsCount.ToString();
                lblRuns.Text = _config.NumRuns.ToString();
                lblLambda.Text = _config.Lambda.ToString("F2");
                lblMu.Text = _config.Mu.ToString("F2");
                lblSigma.Text = _config.Sigma.ToString("F2");
            });
        }
        private void UpdateStatLabel(TextBlock label, string value)
        {
            if (label.CheckAccess())
                label.Text = value;
            else
                Application.Current.Dispatcher.Invoke(() => label.Text = value);
        }

        private void UpdateStatsRealTime(int processed, SimulationEngine engine)
        {
            // обработано заданий
            UpdateStatLabel(lblProcessed, processed.ToString());

            if (engine?.AvgWaitPerPhase == null) return;

            // среднее ожидание (по всем фазам усреднённое)
            double avgWait = engine.AvgWaitPerPhase.Where(v => v > 0).DefaultIfEmpty(0).Average();
            UpdateStatLabel(lblAvgWait, avgWait.ToString("F3"));

            // средняя загрузка
            double avgUtil = engine.UtilizationPerPhase.Where(v => v > 0).DefaultIfEmpty(0).Average();
            UpdateStatLabel(lblAvgUtil, $"{avgUtil:P1}");

            // пропускная способность (текущая)
            if (engine.ThroughputPerTask?.Length > 0 && processed > 0)
            {
                double throughput = engine.ThroughputPerTask[Math.Min(processed - 1, engine.ThroughputPerTask.Length - 1)];
                UpdateStatLabel(lblThroughput, throughput.ToString("F4"));
            }
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
                txtStatus.Text = "Конфигурация обновлена";
            }
        }

        // ГЛАВНАЯ КНОПКА (уже надоело её вечно дописывать)
        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;

            try
            {
                _isRunning = true;
                _cts = new CancellationTokenSource();
                UpdateButtonsState();

                SimulationArea.Visibility = Visibility.Visible;
                PlaceholderArea.Visibility = Visibility.Collapsed;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.Value = 0;

                txtStatus.Text = "Инициализация...";

                // очистка перед запуском
                _tasksCollection.Clear();
                _phasesCollection.Clear();

                // инициализация фаз
                for (int i = 1; i <= _config.PhasesCount; i++)
                    _phasesCollection.Add(new PhaseViewModel { Id = i });

                _engine = new SimulationEngine(_config);

                // подписка на события для обновления UI
                _engine.OnTaskProcessed += OnTaskProcessed;
                _engine.OnRunCompleted += OnRunCompleted;

                _engine.OnTaskProcessed += (current, total) =>
                {
                    // обновление прогресса и списка заданий
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = 100.0 * current / total;
                        txtStatus.Text = $"Обработано: {current}/{total}";

                        var task = _engine.GetTask(current);
                        if (task != null && !_tasksCollection.Any(t => t.Id == task.Id))
                        {
                            _tasksCollection.Add(new TaskViewModel
                            {
                                Id = task.Id,
                                ArrivalTime = task.ArrivalTime,
                                WaitTime = task.TotalWaitTime,
                                Status = current == total ? "Завершено" : "В обработке"
                            });
                        }
                    });
                    UpdateStatsRealTime(current, _engine);
                };

                _engine.OnRunCompleted += (run) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = $"Прогон {run}/{_config.NumRuns} завершён";
                    });
                };

                txtStatus.Text = "Запуск симуляции...";
                // запуск в фоне с поддержкой отмены
                await Task.Run(() => RunSimulationWithProgress(_cts.Token), _cts.Token);

                txtStatus.Text = "Симуляция завершена";
                ProgressBar.Visibility = Visibility.Collapsed;

                // обновляем статистику
                UpdateStatsRealTime(_config.JobsCount, _engine);
                UpdatePhasesStatistics();
            }
            catch (OperationCanceledException)
            {
                txtStatus.Text = "Симуляция остановлена пользователем!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка симуляции!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "ERROR!!!";
            }
            finally
            {
                _isRunning = false;
                _cts?.Dispose();
                _cts = null;
                UpdateButtonsState();
            }
        }

        private void RunSimulationWithProgress(CancellationToken token)
        {
            // подписка на события внутри фонового потока
            _engine!.OnTaskProcessed += (current, total) =>
            {
                // обновление UI через Dispatcher
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = 100.0 * current / total;
                    txtStatus.Text = $"Обработано: {current}/{total}";

                    var task = _engine.GetTask(current);
                    if (task != null && !_tasksCollection.Any(t => t.Id == task.Id))
                    {
                        _tasksCollection.Add(new TaskViewModel
                        {
                            Id = task.Id,
                            ArrivalTime = task.ArrivalTime,
                            WaitTime = task.TotalWaitTime,
                            Status = current == total ? "Завершено" : "В обработке"
                        });
                    }
                });
            };
            // запуск ядра симуляции
            _engine.Run(token);
        }


        private void OnTaskProcessed(int current, int total)
        {
            // обновление прогресс-бара и статуса из фонового потока
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = 100.0 * current / total;
                txtStatus.Text = $"Обработано: {current}/{total}";

                // добавление задания в таблицу (если ещё не добавлено)
                var task = _engine?.GetTask(current);
                if (task != null && !_tasksCollection.Any(t => t.Id == task.Id))
                {
                    _tasksCollection.Add(new TaskViewModel
                    {
                        Id = task.Id,
                        ArrivalTime = task.ArrivalTime,
                        WaitTime = task.TotalWaitTime,
                        Status = current == total ? "Завершено" : "В обработке"
                    });
                }
            });
        }

        private void OnRunCompleted(int runNumber)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                txtStatus.Text = $"Прогон {runNumber}/{_config.NumRuns} завершён";
            });
        }

        private void UpdatePhasesStatistics()
        {
            if (_engine == null) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < _config.PhasesCount; i++)
                {
                    var phase = _phasesCollection.FirstOrDefault(p => p.Id == i + 1);
                    if (phase != null)
                    {
                        phase.Utilization = _engine.UtilizationPerPhase[i];
                        phase.IdlePercent = 1.0 - _engine.UtilizationPerPhase[i];
                        phase.ProcessedCount = _config.JobsCount;
                    }
                }
            });
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning) return;
            _cts?.Cancel();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                btnStop_Click(sender, e);
                return;
            }

            _tasksCollection.Clear();
            _phasesCollection.Clear();
            _engine = null;

            SimulationArea.Visibility = Visibility.Collapsed;
            PlaceholderArea.Visibility = Visibility.Visible;
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.Value = 0;

            txtStatus.Text = "Готов к работе";
            btnResults.IsEnabled = false;
            UpdateButtonsState();
        }


        private void btnResults_Click(object sender, RoutedEventArgs e)
        {
            if (_engine == null)
            {
                MessageBox.Show("Сначала выполните симуляцию!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var chartsWindow = new ChartsWindow(_engine) { Owner = this };
            chartsWindow.ShowDialog();
        }

        //private void LoadTestData()
        //{
        //    var tasks = new[]
        //    {
        //        new { Id = 1, ArrivalTime = 1.2, WaitTime = 0.0, Status = "Завершено" },
        //        new { Id = 2, ArrivalTime = 2.5, WaitTime = 0.3, Status = "В обработке" },
        //        new { Id = 3, ArrivalTime = 3.8, WaitTime = 0.0, Status = "Ожидает" },
        //    };
        //    dgTasks.ItemsSource = tasks;

        //    var phases = new[]
        //    {
        //        new { Id = 1, Utilization = 0.42, IdlePercent = 0.15, ProcessedCount = 85 },
        //        new { Id = 2, Utilization = 0.38, IdlePercent = 0.22, ProcessedCount = 78 },
        //        new { Id = 3, Utilization = 0.45, IdlePercent = 0.12, ProcessedCount = 91 },
        //    };
        //    dgPhases.ItemsSource = phases;
        //}

        // для отображения на главном экране
        public class TaskViewModel
        {
            public int Id { get; set; }
            public double ArrivalTime { get; set; }
            public double WaitTime { get; set; }
            public string Status { get; set; } = "Ожидает";
        }

        public class PhaseViewModel
        {
            public int Id { get; set; }
            public double Utilization { get; set; }
            public double IdlePercent { get; set; }
            public int ProcessedCount { get; set; }
        }
    }
}
