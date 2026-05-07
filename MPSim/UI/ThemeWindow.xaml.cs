using System.Windows;
using System.Windows.Controls;
using MPSim.Theme;

namespace MPSim.UI
{
    public partial class ThemeWindow : Window
    {
        public ThemeWindow()
        {
            InitializeComponent();
        }

        private void btnApplyTheme_Click(object sender, RoutedEventArgs e)
        {
            if (cbTheme.SelectedItem is ComboBoxItem item && item.Tag is string theme)
            {
                ThemeManager.ApplyTheme(theme);
                MessageBox.Show($"Тема \"{item.Content}\" успешно применена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
        }
    }
}