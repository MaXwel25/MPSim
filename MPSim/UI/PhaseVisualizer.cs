using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPSim.Models;

namespace MPSim.UI
{
    public class PhaseVisualizer
    {
        private Panel _container;
        private Label _lblTitle;
        private Label _lblStatus;
        private Label _lblBuffer;
        private ProgressBar _pbProgress;

        public PhaseVisualizer(Panel container)
        {
            _container = container;
            BuildControl();
            ApplyTheme();
        }

        private void BuildControl()
        {
            _container.Size = new Size(150, 200);
            _container.Margin = new Padding(10);

            _lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            _pbProgress = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 15,
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 6
            };

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };

            _lblBuffer = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            _container.Controls.Add(_lblStatus);
            _container.Controls.Add(_pbProgress);
            _container.Controls.Add(_lblTitle);
            _container.Controls.Add(_lblBuffer);
        }

        // сохранение темы
        public void ApplyTheme()
        {
            _container.BackColor = Theme.PhasePanelBg;
            _lblTitle.ForeColor = Theme.TextColor;
            _lblStatus.ForeColor = Theme.TextColor;
            _lblBuffer.BackColor = Theme.BufferBg;
            _lblBuffer.ForeColor = Theme.TextColor;
            // прогресс-бар в WinForms ограничен в кастомизации(
            _pbProgress.BackColor = Theme.PhasePanelBg; // меняется сам фон (BackColor) а не цвет полосы
        }

        public void Update(PhaseState state)
        {
            _lblBuffer.Text = $"Буфер: {state.BufferSize}";
            _lblBuffer.ForeColor = state.BufferSize > 0 ? Color.OrangeRed : Theme.TextColor;

            if (state.IsWorking)
            {
                _lblStatus.Text = "Работает!";
                _lblStatus.ForeColor = Color.LimeGreen;
            }
            else
            {
                _lblStatus.Text = "Простой!";
                _lblStatus.ForeColor = Color.Gray;
            }

            _pbProgress.Value = (int)state.RemainingTime;
            _lblTitle.Text = $"ФУ #{state.Index + 1}";
        }
    }
}
