using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SpotifyOverlay
{
    public static class ThemeManager
    {
        private static readonly string ConfigPath = "settings.json";

        public static bool IsRgbMode { get; set; } = false;
        public static int RgbSpeed { get; set; } = 5; // 1-10 arası hız (saniye cinsinden gecikme değil, tersidir)
        public static int SelectedThemeIndex { get; set; } = 0;

        public static DispatcherTimer RgbTimer;
        private static LinearGradientBrush GradientBrush;
        private static Random Rand = new();
        private static Color CurrentColor1 = Colors.MediumPurple;
        private static Color CurrentColor2 = Colors.CadetBlue;

        public static Brush GetInitialBackground()
        {
            if (IsRgbMode)
            {
                GradientBrush = new LinearGradientBrush(CurrentColor1, CurrentColor2, 45);
                GradientBrush.GradientStops.Add(new GradientStop(CurrentColor1, 0));
                GradientBrush.GradientStops.Add(new GradientStop(CurrentColor2, 1));
                StartRgbAnimation();
                return GradientBrush;
            }

            return SelectedThemeIndex switch
            {
                0 => new SolidColorBrush(Color.FromRgb(34, 34, 34)),  // Koyu Tema
                1 => new SolidColorBrush(Color.FromRgb(230, 230, 230)),  // Açık Tema
                2 => new SolidColorBrush(Color.FromRgb(102, 51, 153)),   // Mor Tema
                3 => new SolidColorBrush(Color.FromRgb(20, 25, 60)),     // Gece Mavisi
                _ => new SolidColorBrush(Color.FromRgb(34, 34, 34)),
            };
        }

        public static void LoadSettings()
        {
            if (!File.Exists(ConfigPath))
            {
                SaveSettings(); // default ayarları kaydet
                return;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<ThemeConfig>(json);
                IsRgbMode = config?.RgbMode ?? false;
                RgbSpeed = config?.RgbSpeed ?? 5;
                SelectedThemeIndex = config?.ThemeIndex ?? 0;
            }
            catch
            {
                // varsayılana dön
                IsRgbMode = false;
                RgbSpeed = 5;
                SelectedThemeIndex = 0;
            }
        }

        public static void SaveSettings()
        {
            var config = new ThemeConfig
            {
                RgbMode = IsRgbMode,
                RgbSpeed = RgbSpeed,
                ThemeIndex = SelectedThemeIndex
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }

        public static void StartRgbAnimation()
        {
            if (GradientBrush == null) return;

            RgbTimer?.Stop();
            RgbTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Math.Clamp(11 - RgbSpeed, 1, 10)) // Hız arttıkça süre kısalır
            };

            RgbTimer.Tick += (_, __) =>
            {
                var newColor1 = GetSimilarColor(CurrentColor1);
                var newColor2 = GetSimilarColor(CurrentColor2);

                AnimateGradientStop(GradientBrush.GradientStops[0], CurrentColor1, newColor1);
                AnimateGradientStop(GradientBrush.GradientStops[1], CurrentColor2, newColor2);

                CurrentColor1 = newColor1;
                CurrentColor2 = newColor2;
            };

            RgbTimer.Start();
        }

        private static Color GetSimilarColor(Color baseColor)
        {
            byte r = (byte)Math.Clamp(baseColor.R + Rand.Next(-15, 16), 0, 255);
            byte g = (byte)Math.Clamp(baseColor.G + Rand.Next(-15, 16), 0, 255);
            byte b = (byte)Math.Clamp(baseColor.B + Rand.Next(-15, 16), 0, 255);
            return Color.FromRgb(r, g, b);
        }

        private static void AnimateGradientStop(GradientStop stop, Color from, Color to)
        {
            var animation = new ColorAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(Math.Clamp(11 - RgbSpeed, 1, 10)),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            stop.BeginAnimation(GradientStop.ColorProperty, animation);
        }

        private class ThemeConfig
        {
            public bool RgbMode { get; set; }
            public int RgbSpeed { get; set; }
            public int ThemeIndex { get; set; }
        }
    }
}
