using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace JoyLauncher.App.Services;

/// <summary>
/// Loads site favicons for known app publishers. A missing or unreachable
/// website icon is non-fatal; the UI keeps its initials-based fallback.
/// </summary>
public sealed class WebsiteLogoService
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    private static readonly ConcurrentDictionary<string, Task<BitmapImage?>> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, string> PublisherDomains =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["com.mojang.minecraftpe"] = "www.minecraft.net",
            ["com.google.android.youtube"] = "www.youtube.com",
            ["com.google.android.apps.youtube.kids"] = "www.youtubekids.com",
            ["com.google.android.apps.youtube.music"] = "music.youtube.com",
            ["com.google.android.videos"] = "tv.google",
            ["com.android.chrome"] = "www.google.com/chrome",
            ["com.google.android.gm"] = "mail.google.com",
            ["com.google.android.apps.maps"] = "maps.google.com",
            ["com.google.android.apps.photos"] = "photos.google.com",
            ["com.android.vending"] = "play.google.com",
            ["com.google.android.googlequicksearchbox"] = "www.google.com"
        };

    public Task<BitmapImage?> GetLogoAsync(string? packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName) ||
            !PublisherDomains.ContainsKey(packageName))
        {
            return Task.FromResult<BitmapImage?>(null);
        }

        return Cache.GetOrAdd(packageName, LoadLogoAsync);
    }

    private static async Task<BitmapImage?> LoadLogoAsync(string packageName)
    {
        try
        {
            var domain = PublisherDomains[packageName];
            var requestUri = new Uri(
                $"https://www.google.com/s2/favicons?domain={Uri.EscapeDataString(domain)}&sz=128");

            using var response = await Client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length == 0 || bytes.Length > 512_000)
                return null;

            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 96;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            IOException or
            NotSupportedException or
            InvalidDataException)
        {
            return null;
        }
    }
}
