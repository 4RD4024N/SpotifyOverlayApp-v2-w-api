using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace SpotifyOverlay
{
    public static class SpotifyAuthManager
    {
        private static readonly string clientId = SpotifySecrets.ClientId;
        private static readonly string clientSecret = SpotifySecrets.ClientSecret;
        private static readonly string redirectUri = SpotifySecrets.RedirectUri;
        private static readonly string TokenPath = "token.json";

        public static string AccessToken { get; private set; }
        public static string RefreshToken { get; private set; }

        public static async Task<bool> AuthenticateAsync()
        {
            // Daha önce kayıtlı token varsa yükle ve yenile
            if (LoadTokens())
            {
                using var http = new HttpClient();
                var refreshRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", RefreshToken),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                });

                var refreshResponse = await http.PostAsync("https://accounts.spotify.com/api/token", refreshRequest);
                var refreshJson = await refreshResponse.Content.ReadAsStringAsync();
                var refreshTokenData = JsonSerializer.Deserialize<JsonElement>(refreshJson);

                AccessToken = refreshTokenData.GetProperty("access_token").GetString();
                return true;
            }

            // İlk yetkilendirme: tarayıcıyı aç
            string authUrl = $"https://accounts.spotify.com/authorize?client_id={clientId}" +
                             $"&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                             $"&scope=user-read-playback-state user-modify-playback-state user-read-currently-playing" +
                             $"&show_dialog=true";

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            // Localhost'ta redirect uri bekle
            var listener = new HttpListener();
            listener.Prefixes.Add(redirectUri.EndsWith("/") ? redirectUri : redirectUri + "/");
            listener.Start();

            var context = await listener.GetContextAsync();
            string code = context.Request.QueryString["code"];

            string responseHtml = "<html><body><h1>Spotify bağlantısı başarılı! Bu pencereyi kapatabilirsin.</h1></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();
            listener.Stop();

            // Access ve Refresh Token'ı al
            using var http2 = new HttpClient();
            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
            });

            var tokenResponse = await http2.PostAsync("https://accounts.spotify.com/api/token", requestBody);
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

            AccessToken = tokenData.GetProperty("access_token").GetString();
            RefreshToken = tokenData.GetProperty("refresh_token").GetString();

            SaveTokens(AccessToken, RefreshToken);
            return true;
        }

        private static void SaveTokens(string accessToken, string refreshToken)
        {
            var data = new
            {
                access_token = accessToken,
                refresh_token = refreshToken
            };

            File.WriteAllText(TokenPath, JsonSerializer.Serialize(data));
        }

        private static bool LoadTokens()
        {
            if (!File.Exists(TokenPath))
                return false;

            var json = File.ReadAllText(TokenPath);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            AccessToken = data.GetProperty("access_token").GetString();
            RefreshToken = data.GetProperty("refresh_token").GetString();

            return true;
        }
    }
}
