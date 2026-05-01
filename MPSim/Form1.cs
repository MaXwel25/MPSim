using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D; // нужно для GraphicsPath (красивые кнопки)

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
            base.OnFormClosed(e);
        }

        private void BuildInterface()
        {
            // верхняя панель
            Panel top = new Panel { Dock = DockStyle.Top, Height = 60 };

            // кнопка старт
            Button btnStart = new Button
            {
                Text = "▶ СТАРТ",
                Location = new Point(20, 10),
                Width = 100,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand // курсор-рука для удобства
            };
            btnStart.Click += async (s, e) =>
            {
                btnStart.Enabled = false;
                await _engine.RunAsync(k: 4, jobsCount: 30, tickDelayMs: 150);
                btnStart.Enabled = true;
            };

            int radius = 15; // радиус скругления
            GraphicsPath path1 = new GraphicsPath();
            path1.StartFigure();
            path1.AddArc(0, 0, radius, radius, 180, 90); // верхний-левый
            path1.AddArc(btnStart.Width - radius, 0, radius, radius, 270, 90); // верхний-правый
            path1.AddArc(btnStart.Width - radius, btnStart.Height - radius, radius, radius, 0, 90); // нижний-правый
            path1.AddArc(0, btnStart.Height - radius, radius, radius, 90, 90); // нижний-левый
            path1.CloseFigure();
            btnStart.Region = new Region(path1);

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

            GraphicsPath path2 = new GraphicsPath();
            path2.StartFigure();
            path2.AddArc(0, 0, radius, radius, 180, 90); // Верхний-левый
            path2.AddArc(btnSettings.Width - radius, 0, radius, radius, 270, 90); // Верхний-правый
            path2.AddArc(btnSettings.Width - radius, btnSettings.Height - radius, radius, radius, 0, 90); // Нижний-правый
            path2.AddArc(0, btnSettings.Height - radius, radius, radius, 90, 90); // Нижний-левый
            path2.CloseFigure();
            btnSettings.Region = new Region(path2);

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

            top.Controls.Add(btnStart);
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