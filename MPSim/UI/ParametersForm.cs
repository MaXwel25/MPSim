using System;
using System.Drawing;
using System.Windows.Forms;

namespace MPSim.UI
{
    public partial class ParametersForm : Form
    {
        public int PhasesCount { get; private set; } = 4;
        public int JobsCount { get; private set; } = 30;
        public int TickDelayMs { get; private set; } = 150;
        public bool UseRandomSeed { get; private set; } = false;
        public int RandomSeed { get; private set; } = 42;
        public string ArrivalDistType { get; private set; } = "Uniform";
        public double ArrivalMin { get; private set; } = 1.0;
        public double ArrivalMax { get; private set; } = 5.0;
        public double ArrivalLambda { get; private set; } = 0.5;
        public string ProcessingDistType { get; private set; } = "Uniform";
        public double ProcessingMin { get; private set; } = 2.0;
        public double ProcessingMax { get; private set; } = 6.0;
        public double ProcessingMean { get; private set; } = 4.0;
        public double ProcessingStdDev { get; private set; } = 1.0;

        public ParametersForm()
        {
            InitializeComponent();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "Параметры симуляции";
            this.Size = new Size(380, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            int y = 15;

            var numK = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 4, Location = new Point(150, y - 2), Width = 80 };
            var lblK = new Label { Text = "Фазы (k):", Location = new Point(15, y), AutoSize = true };
            y += 30;

            var numJobs = new NumericUpDown { Minimum = 5, Maximum = 500, Value = 30, Location = new Point(150, y - 2), Width = 80 };
            var lblJobs = new Label { Text = "Задания (N):", Location = new Point(15, y), AutoSize = true };
            y += 30;

            var numSpeed = new NumericUpDown { Minimum = 50, Maximum = 1000, Value = 150, Location = new Point(150, y - 2), Width = 80 };
            var lblSpeed = new Label { Text = "Задержка (мс):", Location = new Point(15, y), AutoSize = true };
            y += 40;

            var chkSeed = new CheckBox { Text = "Случайный Seed", Location = new Point(15, y), AutoSize = true };
            var numSeed = new NumericUpDown { Minimum = 0, Maximum = 9999, Value = 42, Location = new Point(150, y - 2), Width = 80 };
            chkSeed.CheckedChanged += (s, e) => numSeed.Enabled = !chkSeed.Checked;
            y += 35;

            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(15, y), Width = 100, Height = 35 };
            var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(130, y), Width = 100, Height = 35 };

            btnOk.Click += (s, e) =>
            {
                PhasesCount = (int)numK.Value;
                JobsCount = (int)numJobs.Value;
                TickDelayMs = (int)numSpeed.Value;
                UseRandomSeed = chkSeed.Checked;
                RandomSeed = chkSeed.Checked ? new Random().Next(0, 9999) : (int)numSeed.Value;
            };

            this.Controls.AddRange(new Control[] { lblK, numK, lblJobs, numJobs, lblSpeed, numSpeed, chkSeed, numSeed, btnOk, btnCancel });
        }

        private void ApplyTheme()
        {
            this.BackColor = Theme.FormBg;
            this.ForeColor = Theme.TextColor;
            foreach (Control c in this.Controls)
            {
                c.ForeColor = Theme.TextColor;
                if (c is Button b) { b.BackColor = Theme.ToggleBtnBg; b.ForeColor = Theme.TextColor; }
                else if (c is NumericUpDown n) { n.BackColor = Theme.InputBg; n.ForeColor = Theme.TextColor; }
            }
        }
    }
}