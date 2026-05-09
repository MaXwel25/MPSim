using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace MPSim.Services
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string themeName)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            var baseTheme = themeName.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? BaseTheme.Dark
                : BaseTheme.Light;

            theme.SetBaseTheme(baseTheme);
            paletteHelper.SetTheme(theme);

            System.Diagnostics.Debug.WriteLine($"[ThemeManager] Applied: {baseTheme}");
        }

        public static string ToggleTheme()
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            var currentBase = theme.GetBaseTheme();

            var newBase = currentBase == BaseTheme.Dark ? BaseTheme.Light : BaseTheme.Dark;
            theme.SetBaseTheme(newBase);
            paletteHelper.SetTheme(theme);

            return newBase == BaseTheme.Dark ? "Dark" : "Light";
        }

        public static string GetCurrentTheme()
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            return theme.GetBaseTheme() == BaseTheme.Dark ? "Dark" : "Light";
        }

        public static void ApplyColorScheme(string primaryHex, string secondaryHex = "#03A9F4")
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            if (TryParseColor(primaryHex, out var primary))
            {
                theme.PrimaryMid = CreateColorPair(primary);
                theme.PrimaryLight = CreateColorPair(primary, 0.3);
                theme.PrimaryDark = CreateColorPair(primary, 0.7);
            }

            if (TryParseColor(secondaryHex, out var secondary))
            {
                theme.SecondaryMid = CreateColorPair(secondary);
                theme.SecondaryLight = CreateColorPair(secondary, 0.3);
                theme.SecondaryDark = CreateColorPair(secondary, 0.7);
            }

            paletteHelper.SetTheme(theme);
        }

        #region Helpers

        private static bool TryParseColor(string hex, out Color color)
        {
            color = Colors.Black;
            if (string.IsNullOrWhiteSpace(hex)) return false;
            try
            {
                color = (Color)ColorConverter.ConvertFromString(hex);
                return true;
            }
            catch { return false; }
        }

        private static ColorPair CreateColorPair(Color bg, double opacity = 1.0)
        {
            var finalBg = opacity < 1.0
                ? Color.FromArgb((byte)(bg.A * opacity), bg.R, bg.G, bg.B)
                : bg;
            // автоматический расчёт контрастного цвета текста (чёрный/белый)
            var luminance = 0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B;
            var fg = luminance > 128 ? Colors.Black : Colors.White;
            return new ColorPair(finalBg, fg);
        }

        #endregion
    }
}