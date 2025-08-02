using System;
using System.Windows;
using DotNetEnv;

namespace SpotifyOverlay
{
    public static class SpotifySecrets
    {
        public static string ClientId { get; private set; }
        public static string ClientSecret { get; private set; }
        public static string RedirectUri { get; private set; }

        static SpotifySecrets()
        {
            try
            {
                // Çalışma dizininden bağımsız olarak direkt proje klasöründen oku
                
                DotNetEnv.Env.Load();

                ClientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
                ClientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
                RedirectUri = Environment.GetEnvironmentVariable("REDIRECT_URI");

               
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[.env] yüklenemedi: {ex.Message}");
            }
        }

    }
}
