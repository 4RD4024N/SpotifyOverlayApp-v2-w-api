using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SpotifyOverlay
{
    public class TrackInfo
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string ImageUrl { get; set; }
        public bool IsPlaying { get; set; }
        public int DurationMs { get; set; }
        public int ProgressMs { get; set; }
    }

    public static class SpotifyAPI
    {
        private static readonly HttpClient client = new();

        public static bool IsSpotifyRunning()
        {
            return Process.GetProcessesByName("Spotify").Length > 0;
        }

        public static async Task TogglePlayPauseAsync(string accessToken)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var playback = await GetCurrentlyPlayingAsync(accessToken);
            if (playback == null) return;

            string url = playback.IsPlaying
                ? "https://api.spotify.com/v1/me/player/pause"
                : "https://api.spotify.com/v1/me/player/play";

            await client.PutAsync(url, null);
        }

        public static async Task NextTrackAsync(string accessToken)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            await client.PostAsync("https://api.spotify.com/v1/me/player/next", null);
        }

        public static async Task PreviousTrackAsync(string accessToken)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            await client.PostAsync("https://api.spotify.com/v1/me/player/previous", null);
        }

        public static async Task<TrackInfo> GetCurrentlyPlayingAsync(string accessToken)
        {
            if (!IsSpotifyRunning())
                return null;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var root = JsonSerializer.Deserialize<JsonElement>(json);

            if (!root.TryGetProperty("item", out var item))
                return null;

            var title = item.GetProperty("name").GetString();
            var artist = item.GetProperty("artists")[0].GetProperty("name").GetString();
            var album = item.GetProperty("album").GetProperty("name").GetString();
            var image = item.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString();

            var isPlaying = root.TryGetProperty("is_playing", out var playingProp) && playingProp.GetBoolean();
            var duration = item.GetProperty("duration_ms").GetInt32();
            var progress = root.TryGetProperty("progress_ms", out var progProp) ? progProp.GetInt32() : 0;

            return new TrackInfo
            {
                Title = title,
                Artist = artist,
                Album = album,
                ImageUrl = image,
                IsPlaying = isPlaying,
                DurationMs = duration,
                ProgressMs = progress
            };
        }
    }
}
