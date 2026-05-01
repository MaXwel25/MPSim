using System;
using System.Collections.Generic;
using System.Drawing;
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
        private Button _btnThemeToggle;

        public Form1()
        {
            InitializeComponent();
            this.Text = "MPSim: Анализ конвейера";
            this.Size = new Size(1000, 700);

            _engine = new SimulationEngine();
            _engine.OnUpdate = UpdateUI;

            BuildInterface();
            ApplyThemeToUI(); // применяем тему при старте
        }

        private void BuildInterface()
        {
            // верхняя панель
            Panel top = new Panel { Dock = DockStyle.Top, Height = 60 };

            Button btnStart = new Button { Text = "▶ СТАРТ", Location = new Point(20, 10), Width = 100, Height = 40, FlatStyle = FlatStyle.Flat };
            btnStart.Click += async (s, e) =>
            {
                btnStart.Enabled = false;
                await _engine.RunAsync(k: 4, jobsCount: 30, tickDelayMs: 150);
                btnStart.Enabled = true;
            };

            _btnThemeToggle = new Button { Text = " ТЕМА", Location = new Point(140, 10), Width = 90, Height = 40, FlatStyle = FlatStyle.Flat };
            _btnThemeToggle.Click += (s, e) => ToggleTheme();

            top.Controls.Add(btnStart);
            top.Controls.Add(_btnThemeToggle);
            this.Controls.Add(top);

            // конвейер
            _pipelinePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            this.Controls.Add(_pipelinePanel);

            // статистика (на вывод)
            _gridStats = new DataGridView { Dock = DockStyle.Bottom, Height = 150, RowHeadersVisible = false, AllowUserToAddRows = false };
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

        private void ToggleTheme()
        {
            var newTheme = Theme.CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            Theme.SetTheme(newTheme);
            ApplyThemeToUI();
        }

        private void ApplyThemeToUI()
        {
            // фон формы и панелей
            this.BackColor = Theme.FormBg;
            foreach (Control c in this.Controls)
            {
                if (c is Panel p && c.Dock == DockStyle.Top)
                {
                    p.BackColor = Theme.TopPanelBg;
                    foreach (Control child in p.Controls)
                    {
                        if (child is Button btn)
                        {
                            btn.BackColor = btn.Text.Contains("ТЕМА") ? Theme.ToggleBtnBg : Theme.ButtonBg;
                            btn.ForeColor = btn.Text.Contains("ТЕМА") ? Theme.TextColor : Theme.ButtonText;
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