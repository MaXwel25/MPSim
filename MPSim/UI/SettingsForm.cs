using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MPSim.UI;

namespace MPSim
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            ApplyTheme();

            // подписываемся на глобальную смену темы 
            Theme.ThemeChanged += ApplyTheme;
            this.FormClosed += (s, e) => Theme.ThemeChanged -= ApplyTheme;
        }

        private void InitializeComponent()
        {
            this.Text = "Настройки приложения";
            this.Size = new Size(320, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.FormBg;

            // выбор темы
            RadioButton rbDark = new RadioButton
            { Text = "Тёмная тема", Location = new Point(20, 20), AutoSize = true, Checked = Theme.CurrentTheme == AppTheme.Dark };

            RadioButton rbLight = new RadioButton
            { Text = "Светлая тема", Location = new Point(20, 50), AutoSize = true, Checked = Theme.CurrentTheme == AppTheme.Light };

            rbDark.CheckedChanged += (s, e) => { if (rbDark.Checked) Theme.SetTheme(AppTheme.Dark); };
            rbLight.CheckedChanged += (s, e) => { if (rbLight.Checked) Theme.SetTheme(AppTheme.Light); };

            // ссылка на мой GitHub)
            LinkLabel lnkGitHub = new LinkLabel
            {
                Text = "GitHub автора проекта",
                Location = new Point(20, 100),
                AutoSize = true,
                LinkColor = Color.DodgerBlue,
                VisitedLinkColor = Color.Purple
            };
            lnkGitHub.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo("https://github.com/MaXwel25") { UseShellExecute = true });

            // версия
            Label lblVersion = new Label
            {
                Text = $"Версия: {Application.ProductVersion} ",
                Location = new Point(20, 140),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            // кнопка закрытия
            Button btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(20, 180),
                Width = 120,
                Height = 35,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { rbDark, rbLight, lnkGitHub, lblVersion, btnClose });
        }

        private void ApplyTheme()
        {
            this.BackColor = Theme.FormBg;
            this.ForeColor = Theme.TextColor;

            foreach (Control c in this.Controls)
            {
                c.ForeColor = Theme.TextColor;
                if (c is Button btn)
                {
                    btn.BackColor = Theme.ToggleBtnBg;
                    btn.ForeColor = Theme.TextColor;
                }
                else if (c is RadioButton rb)
                {
                    rb.BackColor = Color.Transparent;
                }
                else if (c is Label lbl && lbl.Text.Contains("Версия"))
                {
                    lbl.ForeColor = Color.Gray;
                }
            }
        }
    }
}
