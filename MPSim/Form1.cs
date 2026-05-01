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

        private Button _btnStart;
        private Button _btnStop;

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

            int radius = 15;
            GraphicsPath pathStart = new GraphicsPath();
            pathStart.StartFigure();
            pathStart.AddArc(0, 0, radius, radius, 180, 90);
            pathStart.AddArc(_btnStart.Width - radius, 0, radius, radius, 270, 90);
            pathStart.AddArc(_btnStart.Width - radius, _btnStart.Height - radius, radius, radius, 0, 90);
            pathStart.AddArc(0, _btnStart.Height - radius, radius, radius, 90, 90);
            pathStart.CloseFigure();
            _btnStart.Region = new Region(pathStart);

            // 2. Кнопка СТОП (сразу справа от СТАРТ)
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

            GraphicsPath pathStop = new GraphicsPath();
            pathStop.StartFigure();
            pathStop.AddArc(0, 0, radius, radius, 180, 90);
            pathStop.AddArc(_btnStop.Width - radius, 0, radius, radius, 270, 90);
            pathStop.AddArc(_btnStop.Width - radius, _btnStop.Height - radius, radius, radius, 0, 90);
            pathStop.AddArc(0, _btnStop.Height - radius, radius, radius, 90, 90);
            pathStop.CloseFigure();
            _btnStop.Region = new Region(pathStop);

            // кнопка настройки
            Button btnSettings = new Button
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

            GraphicsPath pathSettings = new GraphicsPath();
            pathSettings.StartFigure();
            pathSettings.AddArc(0, 0, radius, radius, 180, 90);
            pathSettings.AddArc(btnSettings.Width - radius, 0, radius, radius, 270, 90);
            pathSettings.AddArc(btnSettings.Width - radius, btnSettings.Height - radius, radius, radius, 0, 90);
            pathSettings.AddArc(0, btnSettings.Height - radius, radius, radius, 90, 90);
            pathSettings.CloseFigure();
            btnSettings.Region = new Region(pathSettings);

            //GraphicsPath path = new GraphicsPath();
            //// создаем эллипс по размеру кнопки
            //path.AddEllipse(0, 0, btnSettings.Width, btnSettings.Height);
            //// применяем эту форму как область видимости/кликабельности кнопки
            //btnSettings.Region = new Region(path);

            btnSettings.Click += (s, e) =>
            {
                // открываем модальное окно настроек
                using var settingsForm = new SettingsForm();
                settingsForm.ShowDialog(this);
            };

            top.Controls.Add(_btnStart);
            top.Controls.Add(_btnStop);
            top.Controls.Add(btnSettings);
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

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;

            _cts = new CancellationTokenSource();
            _isRunning = true;

            _btnStart.Enabled = false;
            _btnStop.Enabled = true;

            try
            {
                // запуск с поддержкой отмены
                await _engine.RunAsync(k: 4, jobsCount: 30, tickDelayMs: 150, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // нормальное завершение при нажатии СТОП
            }
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