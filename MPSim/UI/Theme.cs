using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace MPSim.UI
{
    public enum AppTheme { Light, Dark }

    public static class Theme
    {
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

        // глобальное событие для мгновенного обновления ui
        public static event Action ThemeChanged;

        public static void SetTheme(AppTheme theme)
        {
            if (CurrentTheme != theme)
            {
                CurrentTheme = theme;
                ThemeChanged?.Invoke(); // уведомление об изменение темы
            }
        }

        // цвета (для всего приложения) в 2-ух темах
        public static Color FormBg => CurrentTheme == AppTheme.Dark ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 245, 245);
        public static Color TopPanelBg => CurrentTheme == AppTheme.Dark ? Color.FromArgb(40, 40, 40) : Color.FromArgb(230, 230, 230);
        public static Color PhasePanelBg => CurrentTheme == AppTheme.Dark ? Color.FromArgb(50, 50, 50) : Color.White;
        public static Color TextColor => CurrentTheme == AppTheme.Dark ? Color.White : Color.Black;
        public static Color BufferBg => CurrentTheme == AppTheme.Dark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(240, 240, 240);
        public static Color GridBg => CurrentTheme == AppTheme.Dark ? Color.FromArgb(40, 40, 40) : Color.White;
        public static Color GridHeaderBg => CurrentTheme == AppTheme.Dark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(240, 240, 240);
        public static Color ButtonBg => CurrentTheme == AppTheme.Dark ? Color.LimeGreen : Color.FromArgb(0, 120, 215);
        public static Color ButtonText => CurrentTheme == AppTheme.Dark ? Color.Black : Color.White;
        public static Color ToggleBtnBg => CurrentTheme == AppTheme.Dark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);
    }
}
