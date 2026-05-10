using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        private readonly ObservableCollection<TaskViewModel> _tasksCollection = new();
        private readonly ObservableCollection<PhaseViewModel> _phasesCollection = new();

        private readonly ObservableCollection<TaskByPhaseViewModel> _tasksByPhaseCollection = new();
        private int _selectedPhaseId = 0;



        public MainWindow()
        {
            InitializeComponent();
            _config = new SimulationConfig();
            //_tasksCollection = new ObservableCollection<TaskViewModel>();
            //_phasesCollection = new ObservableCollection<PhaseViewModel>();

            //dgTasks.ItemsSource = _tasksCollection;
            dgPhases.ItemsSource = _phasesCollection;

            dgTasksByPhase.ItemsSource = _tasksByPhaseCollection;

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

        //private DateTime _lastStatsUpdate = DateTime.MinValue;
        //private const int STATS_UPDATE_INTERVAL_MS = 100; // обновлять не чаще 10 раз в секунду (для оптимизации UI)


        private void UpdateStatsRealTime(int processed, SimulationEngine engine)
        {
            // нерабочая оптимизация (проблема с отображением статистики)
            //var now = DateTime.Now;
            //if ((now - _lastStatsUpdate).TotalMilliseconds < STATS_UPDATE_INTERVAL_MS) // считаем время
            //    return;

            //_lastStatsUpdate = now;

            // обработано заданий
            UpdateStatLabel(lblProcessed, processed.ToString());

            if (engine?.AvgWaitPerPhase == null) return;

            // среднее ожидание (по всем фазам усреднённое)
            double avgWait = engine.AvgWaitPerPhase.Where(v => v > 0).DefaultIfEmpty(0).Average();
            UpdateStatLabel(lblAvgWait, avgWait.ToString("F3"));

            // средняя загрузка
            double avgUtil = engine.UtilizationPerPhase.Where(v => v > 0).DefaultIfEmpty(0).Average();
            UpdateStatLabel(lblAvgUtil, $"{avgUtil:P1}");

            double throughput = engine.GetCurrentThroughput();
            UpdateStatLabel(lblThroughput, throughput.ToString("F4"));

            // пропускная способность (текущая)
            if (engine.ThroughputPerTask?.Length > 0 && processed > 0)
            {
                double urrentthroughput = engine.ThroughputPerTask[Math.Min(processed - 1, engine.ThroughputPerTask.Length - 1)];
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
                UpdateConfigDisplay();
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

                if (_tasksCollection == null || _phasesCollection == null)
                {
                    MessageBox.Show("Коллекции не инициализированы!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // очистка перед запуском
                _tasksCollection.Clear();
                _phasesCollection.Clear();

                // инициализация фаз
                for (int i = 1; i <= _config.PhasesCount; i++)
                    _phasesCollection.Add(new PhaseViewModel { Id = i });

                _engine = new SimulationEngine(_config);

                InitializePhaseFilter();

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
                                Status = "В обработке"
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
                UpdateTasksByPhase();

                foreach (var task in _tasksCollection)
                    task.Status = "Завершено";

                // обновляем статистику
                UpdateStatsRealTime(_config.JobsCount, _engine);
                UpdatePhasesStatistics();

                UpdateTasksByPhase();
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

        // метод инициализации фильтра по фазам
        private void InitializePhaseFilter()
        {
            cbPhaseFilter.Items.Clear();
            cbPhaseFilter.Items.Add(new ComboBoxItem { Content = "Все фазы", Tag = 0 });

            for (int i = 1; i <= _config.PhasesCount; i++)
            {
                cbPhaseFilter.Items.Add(new ComboBoxItem
                {
                    Content = $"Фаза {i}",
                    Tag = i
                });
            }

            if (cbPhaseFilter.Items.Count > 0)
                cbPhaseFilter.SelectedIndex = 0;
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

            _tasksCollection?.Clear();
            _phasesCollection?.Clear();
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

        // метод инициализации фильтра по фазам

        private void UpdateTasksByPhase()
        {
            _tasksByPhaseCollection.Clear();

            if (_engine == null) return;

            var selectedPhase = _selectedPhaseId;

            foreach (var taskVm in _tasksCollection)
            {
                var task = _engine.GetTask(taskVm.Id);
                if (task == null) continue;

                if (selectedPhase == 0)
                {
                    // показываем все фазы всех заданий
                    for (int i = 0; i < task.PhaseCount; i++)
                    {
                        _tasksByPhaseCollection.Add(new TaskByPhaseViewModel
                        {
                            Id = task.Id,
                            SelectedPhaseId = i + 1,
                            PhaseStartTime = task.StartTimes[i],
                            PhaseFinishTime = task.FinishTimes[i],
                            PhaseWaitTime = task.WaitTimes[i],
                            PhaseProcessingTime = task.ProcessingTimes[i]
                        });
                    }
                }
                else if (selectedPhase <= task.PhaseCount)
                {
                    // показываем только выбранную фазу
                    int idx = selectedPhase - 1;
                    _tasksByPhaseCollection.Add(new TaskByPhaseViewModel
                    {
                        Id = task.Id,
                        SelectedPhaseId = selectedPhase,
                        PhaseStartTime = task.StartTimes[idx],
                        PhaseFinishTime = task.FinishTimes[idx],
                        PhaseWaitTime = task.WaitTimes[idx],
                        PhaseProcessingTime = task.ProcessingTimes[idx]
                    });
                }
            }

            // обновляем статистику
            UpdatePhaseFilterStats();
        }

        private void UpdatePhaseFilterStats()
        {
            int totalTasks = _tasksByPhaseCollection.Select(t => t.Id).Distinct().Count();
            double avgWait = _tasksByPhaseCollection.Count > 0
                ? _tasksByPhaseCollection.Average(t => t.PhaseWaitTime)
                : 0;

            lblPhaseStats.Text = $"Заданий: {totalTasks}, Ср. ожидание: {avgWait:F3}";
        }

        // обработчик изменения выбора фазы
        private void cbPhaseFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbPhaseFilter?.SelectedItem is ComboBoxItem item)
            {
                _selectedPhaseId = item.Tag is int tag ? tag : 0;
                UpdateTasksByPhase();
            }
        }

        // для отображения на главном экране
        public class TaskViewModel : INotifyPropertyChanged
        {
            private string _status = "Ожидает";
            private int _currentPhase = 0;

            public int Id { get; set; }
            public double ArrivalTime { get; set; }
            public double WaitTime { get; set; }
            public double TotalWaitTime { get; set; }
            public double TotalProcessingTime { get; set; }
            public double FinishTime { get; set; }

            public List<PhaseDetailViewModel> PhaseDetails { get; set; } = new();

            public int CurrentPhase
            {
                get => _currentPhase;
                set { _currentPhase = value; OnPropertyChanged(); }
            }


            public string Status
            {
                get => _status;
                set { _status = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        // модель для отображения информации по конкретной фазе
        public class PhaseDetailViewModel
        {
            public int PhaseId { get; set; }
            public double StartTime { get; set; }
            public double FinishTime { get; set; }
            public double WaitTime { get; set; }
            public double ProcessingTime { get; set; }
        }

        // модель для отображения в таблице при выборе фазы
        public class TaskByPhaseViewModel
        {
            public int Id { get; set; }
            public double PhaseStartTime { get; set; }
            public double PhaseFinishTime { get; set; }
            public double PhaseWaitTime { get; set; }
            public double PhaseProcessingTime { get; set; }
            public int SelectedPhaseId { get; set; }
        }


        public class PhaseViewModel : INotifyPropertyChanged
        {
            private double _utilization;
            private double _idlePercent;
            private int _processedCount;

            public int Id { get; set; }

            public double Utilization
            {
                get => _utilization;
                set { _utilization = value; OnPropertyChanged(); }
            }

            public double IdlePercent
            {
                get => _idlePercent;
                set { _idlePercent = value; OnPropertyChanged(); }
            }

            public int ProcessedCount
            {
                get => _processedCount;
                set { _processedCount = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
