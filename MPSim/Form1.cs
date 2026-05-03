using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D; // нужно для GraphicsPath (красивые кнопки)
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MPSim.Models;
using MPSim.Services;
using MPSim.UI;

namespace MPSim
{
    public partial class Form1 : Form
    {
        private SimulationEngine _engine;
        private FlowLayoutPanel _pipelinePanel;
        private List<PhaseVisualizer> _visualizers = new List<PhaseVisualizer>();
        private DataGridView _gridStats;

        private CancellationTokenSource _cts; // для остановки
        private bool _isRunning = false; // состояния конвейера
        private Button _btnStart, _btnStop, _btnSettings, _btnExport, _btnParams;

        private SimulationParameters _currentParams = new SimulationParameters();

        public Form1()
        {
            InitializeComponent();
            this.Text = "MPSim: Анализ конвейера";
            this.Size = new Size(1000, 700);


            _engine = new SimulationEngine();
            _engine.OnUpdate = UpdateUI;

            // подписка на глобальное событие смены темы
            Theme.ThemeChanged += ApplyThemeToUI;

            BuildInterface();
            ApplyThemeToUI();
        }

        // отписываемся от события при закрытии формы, чтобы избежать утечек памяти
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Theme.ThemeChanged -= ApplyThemeToUI;
            _cts?.Cancel(); // гарантированная остановка при закрытии
            base.OnFormClosed(e);
        }

        private void BuildInterface()
        {
            // верхняя панель
            Panel top = new Panel { Dock = DockStyle.Top, Height = 60 };
            int radius = 15;

            // кнопка старт
            _btnStart = new Button
            {
                Text = "▶ СТАРТ",
                Location = new Point(20, 10),
                Width = 100,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand // курсор-рука для удобства
            };
            _btnStart.Click += BtnStart_Click;
            _btnStart.Region = CreateRoundedRegion(_btnStart, radius);

            // кнопка стоп
            _btnStop = new Button
            {
                Text = "⏹ СТОП",
                Location = new Point(130, 10), // 20 + 100 + 10 отступ
                Width = 100,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Enabled = false // изначально неактивна
            };
            _btnStop.Click += BtnStop_Click;
            _btnStop.Region = CreateRoundedRegion(_btnStop, radius);

            // кнопка экспорт
            _btnExport = new Button
            {
                Text = "📊 Экспорт",
                Location = new Point(240, 10),
                Width = 100,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnExport.Click += BtnExport_Click;
            _btnExport.Region = CreateRoundedRegion(_btnExport, radius);

            // кнопка параметров симуляции
            _btnParams = new Button
            {
                Text = "⚙ ПАРАМЕТРЫ",
                Location = new Point(350, 10),
                Width = 130,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnParams.Click += BtnParams_Click;
            _btnParams.Region = CreateRoundedRegion(_btnParams, radius);

            // кнопка настройки
            _btnSettings = new Button
            {
                Text = "НАСТРОЙКИ",
                Location = new Point(60, 10),
                Width = 120,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,  // фиксируем справа
                Cursor = Cursors.Hand, // курсор-рука для удобства
                FlatAppearance = { BorderSize = 0 }
            };
            _btnSettings.Region = CreateRoundedRegion(_btnSettings, radius);

            //GraphicsPath path = new GraphicsPath();
            //// создаем эллипс по размеру кнопки
            //path.AddEllipse(0, 0, btnSettings.Width, btnSettings.Height);
            //// применяем эту форму как область видимости/кликабельности кнопки
            //btnSettings.Region = new Region(path);

            _btnSettings.Click += (s, e) =>
            {
                // открываем модальное окно настроек
                using var settingsForm = new SettingsForm();
                settingsForm.ShowDialog(this);
            };

            top.Controls.Add(_btnStart);
            top.Controls.Add(_btnStop);
            top.Controls.Add(_btnExport);
            top.Controls.Add(_btnSettings);
            top.Controls.Add(_btnParams);
            this.Controls.Add(top);

            // панель визуализации
            _pipelinePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true
            };
            this.Controls.Add(_pipelinePanel);

            // таблицы статистики
            _gridStats = new DataGridView
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                RowHeadersVisible = false,
                AllowUserToAddRows = false
            };
            _gridStats.Columns.Add("Phase", "Фаза");
            _gridStats.Columns.Add("Idle", "Простой (с)");
            _gridStats.Columns.Add("Jobs", "Заданий");
            this.Controls.Add(_gridStats);

            // создаем 4 фазы (для начала как тестовую)
            for (int i = 0; i < 4; i++)
            {
                var panel = new Panel();
                _pipelinePanel.Controls.Add(panel);
                _visualizers.Add(new PhaseVisualizer(panel));
            }
        }

        // отдельно для скругления кнопок
        private Region CreateRoundedRegion(Button btn, int radius)
        {
            var path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(btn.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(btn.Width - radius, btn.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, btn.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return new Region(path);
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;

            _cts = new CancellationTokenSource();
            _isRunning = true;

            _btnStart.Enabled = false;
            _btnStop.Enabled = true;

            try
            {
                // создаём параметры распределений перед вызовом
                var arrivalParams = new DistributionParams
                {
                    Type = _currentParams.ArrivalDistType,
                    Min = _currentParams.ArrivalMin,
                    Max = _currentParams.ArrivalMax,
                    Lambda = _currentParams.ArrivalLambda
                };

                var processingParams = new DistributionParams
                {
                    Type = _currentParams.ProcessingDistType,
                    Min = _currentParams.ProcessingMin,
                    Max = _currentParams.ProcessingMax,
                    Mean = _currentParams.ProcessingMean,
                    StdDev = _currentParams.ProcessingStdDev
                };

                // теперь передаём все 7 параметров (включая созданные выше)
                await _engine.RunAsync(
                    k: _currentParams.PhasesCount,
                    jobsCount: _currentParams.JobsCount,
                    tickDelayMs: _currentParams.TickDelayMs,
                    seed: _currentParams.UseRandomSeed ? null : _currentParams.RandomSeed,
                    arrivalParams: arrivalParams,        // переменная создана выше
                    processingParams: processingParams,  // переменная создана выше
                    cancellationToken: _cts.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRunning = false;
                _btnStart.Enabled = true;
                _btnStop.Enabled = false;
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "CSV файлы|*.csv|Все файлы|*.*",
                FileName = $"MPSim_Results_{DateTime.Now:yyyyMMdd_HHmmss}.csv" // по текущей дате
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // получаем текущее состояние фаз
                    var currentStates = new List<PhaseState>();
                    foreach (var vis in _visualizers)
                    {
                        // здесь нужно получить актуальное состояние из визуализаторов
                        // для упрощения создаём заглушку — в реальном коде передавайте из UpdateUI
                        currentStates.Add(new PhaseState { Index = 0, JobsProcessed = 0, TotalWaitTime = 0, TotalIdleTime = 0 });
                    }
                    ExportService.ExportFullReport(
                        saveDialog.FileName,
                        currentStates, // замените на актуальные states
                        _engine.JobHistory,
                        _currentParams,
                        _engine.TotalSimulationTime);


                    MessageBox.Show("Отчёт успешно сохранён!", "Экспорт",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Экспорт",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnParams_Click(object sender, EventArgs e)
        {
            using var paramsForm = new ParametersForm();
            if (paramsForm.ShowDialog(this) == DialogResult.OK)
            {
                _currentParams = new SimulationParameters
                {
                    PhasesCount = paramsForm.PhasesCount,
                    JobsCount = paramsForm.JobsCount,
                    TickDelayMs = paramsForm.TickDelayMs,
                    UseRandomSeed = paramsForm.UseRandomSeed,
                    RandomSeed = paramsForm.RandomSeed,
                    ArrivalDistType = paramsForm.ArrivalDistType,
                    ArrivalMin = paramsForm.ArrivalMin,
                    ArrivalMax = paramsForm.ArrivalMax,
                    ArrivalLambda = paramsForm.ArrivalLambda,
                    ProcessingDistType = paramsForm.ProcessingDistType,
                    ProcessingMin = paramsForm.ProcessingMin,
                    ProcessingMax = paramsForm.ProcessingMax,
                    ProcessingMean = paramsForm.ProcessingMean,
                    ProcessingStdDev = paramsForm.ProcessingStdDev
                };

                this.Text = $"MPSim | k={_currentParams.PhasesCount} | N={_currentParams.JobsCount}";

                if (!_isRunning && _currentParams.PhasesCount != _visualizers.Count)
                    RebuildPhases(_currentParams.PhasesCount);
            }
        }

        private void RebuildPhases(int newK)
        {
            _pipelinePanel.Controls.Clear();
            _visualizers.Clear();
            for (int i = 0; i < newK; i++)
            {
                var panel = new Panel();
                _pipelinePanel.Controls.Add(panel);
                _visualizers.Add(new PhaseVisualizer(panel));
            }
            ApplyThemeToUI();
        }

        // применяет текущую тему ко всем элементам формы
        private void ApplyThemeToUI()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ApplyThemeToUI));
                return;
            }

            // фон формы
            this.BackColor = Theme.FormBg;

            // верхняя панель и кнопки
            foreach (Control c in this.Controls)
            {
                if (c is Panel p && c.Dock == DockStyle.Top)
                {
                    p.BackColor = Theme.TopPanelBg;
                    foreach (Control child in p.Controls)
                    {
                        if (child is Button btn)
                        {
                            btn.BackColor = btn.Text.Contains("НАСТРОЙКИ")
                                ? Theme.ToggleBtnBg
                                : Theme.ButtonBg;
                            btn.ForeColor = btn.Text.Contains("НАСТРОЙКИ")
                                ? Theme.TextColor
                                : Theme.ButtonText;
                        }
                    }
                }
            }

            // таблица фаз
            _gridStats.BackgroundColor = Theme.GridBg;
            _gridStats.DefaultCellStyle.BackColor = Theme.GridBg;
            _gridStats.DefaultCellStyle.ForeColor = Theme.TextColor;
            _gridStats.ColumnHeadersDefaultCellStyle.BackColor = Theme.GridHeaderBg;
            _gridStats.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextColor;

            // Визуализаторы фаз
            foreach (var vis in _visualizers)
                vis.ApplyTheme();
        }

        private void UpdateUI(List<PhaseState> states)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateUI(states)));
                return;
            }

            for (int i = 0; i < states.Count; i++)
                _visualizers[i].Update(states[i]);

            _gridStats.Rows.Clear();
            foreach (var st in states)
                _gridStats.Rows.Add($"Фаза {st.Index + 1}", st.TotalIdleTime, st.JobsProcessed);
        }
    }
}