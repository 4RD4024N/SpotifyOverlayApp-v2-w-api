using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SpotifyOverlay
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer hideTimer;
        private DispatcherTimer timer;
        private string lastTrackId = null;
        private bool lastIsPlaying = false;
        private int lastVolume = -1;
        private bool isSettingsWindowOpen = false;

        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.LoadSettings();
            rootBorder.Background = ThemeManager.GetInitialBackground();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            bool success = await SpotifyAuthManager.AuthenticateAsync();
            if (!success)
            {
                MessageBox.Show("Spotify bağlantısı başarısız.");
                Close();
                return;
            }

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += async (_, __) => await UpdateTrackInfo();
            timer.Start();

            hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            hideTimer.Tick += (_, __) =>
            {
                hideTimer.Stop();
                if (isSettingsWindowOpen) return; // Ayar penceresi açıksa gizlenme
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
                fadeOut.Completed += (_, __) => this.Visibility = Visibility.Hidden;
                BeginAnimation(Window.OpacityProperty, fadeOut);
            };

            await UpdateTrackInfo();
        }
        public void ApplyTheme()
        {
            ThemeManager.LoadSettings();
            rootBorder.Background = ThemeManager.GetInitialBackground();
        }

        private async Task UpdateTrackInfo()
        {
            var info = await SpotifyAPI.GetCurrentlyPlayingAsync(SpotifyAuthManager.AccessToken);
            if (info == null || string.IsNullOrEmpty(info.Title))
            {
                this.Visibility = Visibility.Hidden;
                return;
            }

            int vol = -1;
            try { vol = VolumeManager.GetMasterVolume(); } catch { }

            bool trackChanged = info.Title != lastTrackId;
            bool playbackChanged = info.IsPlaying != lastIsPlaying;
            bool volumeChanged = vol != lastVolume;

            if (trackChanged || playbackChanged || volumeChanged)
            {
                if (this.Visibility != Visibility.Visible)
                {
                    this.Opacity = 0;
                    this.Visibility = Visibility.Visible;
                    BeginAnimation(Window.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
                }

                hideTimer.Stop();
                hideTimer.Start();
            }

            if (trackChanged)
            {
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
                fadeOut.Completed += (_, __) =>
                {
                    songTitleText.Text = info.Title;
                    artistText.Text = info.Artist;
                    albumText.Text = info.Album;

                    if (!string.IsNullOrWhiteSpace(info.ImageUrl))
                    {
                        try
                        {
                            string uniqueUrl = info.ImageUrl + "?t=" + DateTime.Now.Ticks;

                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(uniqueUrl);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                            bitmap.EndInit();

                            albumCover.Source = bitmap;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Albüm kapağı yüklenemedi: " + ex.Message);
                        }
                    }

                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                    {
                        EasingFunction = new SineEase()
                    };

                    songTitleText.BeginAnimation(OpacityProperty, fadeIn);
                    artistText.BeginAnimation(OpacityProperty, fadeIn);
                    albumText.BeginAnimation(OpacityProperty, fadeIn);
                    albumCover.BeginAnimation(OpacityProperty, fadeIn);
                };

                songTitleText.BeginAnimation(OpacityProperty, fadeOut);
                artistText.BeginAnimation(OpacityProperty, fadeOut);
                albumText.BeginAnimation(OpacityProperty, fadeOut);
                albumCover.BeginAnimation(OpacityProperty, fadeOut);
            }

            playPauseButton.Content = info.IsPlaying ? "⏸" : "▶️";

            TimeSpan elapsed = TimeSpan.FromMilliseconds(info.ProgressMs);
            TimeSpan total = TimeSpan.FromMilliseconds(info.DurationMs);
            elapsedTimeText.Text = elapsed.ToString(@"mm\:ss");
            totalTimeText.Text = total.ToString(@"mm\:ss");

            double progress = info.DurationMs > 0 ? (double)info.ProgressMs / info.DurationMs * 100 : 0;
            progressBar.Value = progress;

            if (vol >= 0)
            {
                var anim = new DoubleAnimation
                {
                    To = vol,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new SineEase()
                };
                volumeBar.BeginAnimation(ProgressBar.ValueProperty, anim);

                volumeIcon.Text = vol == 0 ? "🔇" : vol >= 95 ? "🔊" : "🔉";

                if (vol >= 95)
                {
                    volumeBar.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Red,
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };
                }
                else volumeBar.Effect = null;
            }

            lastTrackId = info.Title;
            lastIsPlaying = info.IsPlaying;
            lastVolume = vol;
        }

        private void AnimateButtonBounce(Button button)
        {
            var scale = new ScaleTransform(1, 1);
            button.RenderTransformOrigin = new Point(0.5, 0.5);
            button.RenderTransform = scale;

            var anim = new DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true,
                EasingFunction = new BounceEase { Bounces = 1, Bounciness = 2 }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWindow();
            settings.Owner = this;
            settings.WindowStartupLocation = WindowStartupLocation.Manual;
            var bottomLeft = this.PointToScreen(new Point(0, this.ActualHeight));
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                var m = source.CompositionTarget.TransformFromDevice;
                bottomLeft = m.Transform(bottomLeft);
            }
            settings.Left = bottomLeft.X;
            settings.Top = bottomLeft.Y;
            isSettingsWindowOpen = true;
            settings.Closed += (_, __) =>
            {
                isSettingsWindowOpen = false;
                hideTimer.Stop();
                hideTimer.Start();
            };
            settings.Show();
        }

        private async void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            await SpotifyAPI.TogglePlayPauseAsync(SpotifyAuthManager.AccessToken);
            // AnimateButtonBounce(playPauseButton); // Animasyonu kaldırdık
            await UpdateTrackInfo();
        }
        
        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            await SpotifyAPI.NextTrackAsync(SpotifyAuthManager.AccessToken);
            await UpdateTrackInfo();
        }

        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            await SpotifyAPI.PreviousTrackAsync(SpotifyAuthManager.AccessToken);
            await UpdateTrackInfo();
        }
    }
}