using System;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace SpotifyOverlay
{
    public partial class SettingsWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public SettingsWindow()
        {
            InitializeComponent();

            // Click-through stilini kaldır (tıklanabilir yapar)
            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);

            // Ayarları yükle
            rgbCheckbox.IsChecked = ThemeManager.IsRgbMode;
            rgbSpeedSlider.Value = ThemeManager.RgbSpeed;
            themeComboBox.SelectedIndex = ThemeManager.SelectedThemeIndex;

            // Olayları bağla
            rgbCheckbox.Checked += RgbToggled;
            rgbCheckbox.Unchecked += RgbToggled;
            themeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;

            // UI güncelle
            UpdateRgbControls();
        }

        private void RgbToggled(object sender, RoutedEventArgs e)
        {
            UpdateRgbControls();
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // "RGB (Animasyonlu)" son öğe ise checkbox otomatik aktifleştir
            bool isRgbTheme = themeComboBox.SelectedIndex == themeComboBox.Items.Count - 1;
            rgbCheckbox.IsChecked = isRgbTheme;
            UpdateRgbControls();
        }
        public void ApplyTheme()
        {
            ThemeManager.LoadSettings();
            rootBorder.Background = ThemeManager.GetInitialBackground();
        }

        private void UpdateRgbControls()
        {
            bool showRgb = rgbCheckbox.IsChecked == true
                           || themeComboBox.SelectedIndex == themeComboBox.Items.Count - 1;
            rgbSpeedPanel.Visibility = showRgb
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Tema ayarlarını kaydet
            ThemeManager.IsRgbMode = rgbCheckbox.IsChecked == true
                                     || themeComboBox.SelectedIndex == themeComboBox.Items.Count - 1;
            ThemeManager.RgbSpeed = (int)rgbSpeedSlider.Value;
            ThemeManager.SelectedThemeIndex = themeComboBox.SelectedIndex;
            ThemeManager.SaveSettings();

            // Ana pencerede temayı uygula
            if (this.Owner is MainWindow main)
            {
                main.ApplyTheme();
            }

            Close();
        }
    }
}
