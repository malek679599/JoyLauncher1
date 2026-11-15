using System.Globalization;
using System.Text.RegularExpressions;

namespace JoyLauncher.App.Services;

public sealed record AppMetadata(string Name, string Category, string Description);

public static class AppMetadataCatalog
{
    private static readonly IReadOnlyDictionary<string, AppMetadata> KnownPackages =
        new Dictionary<string, AppMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["com.mojang.minecraftpe"] = new("Minecraft", "Games", "Build, explore, and create in Minecraft."),
            ["com.google.android.youtube"] = new("YouTube", "Entertainment", "Video and creator content."),
            ["com.google.android.apps.youtube.kids"] = new("YouTube Kids", "Entertainment", "Family-focused video content."),
            ["com.google.android.apps.youtube.music"] = new("YouTube Music", "Entertainment", "Music and audio streaming."),
            ["com.google.android.videos"] = new("Google TV", "Entertainment", "Movies and television."),
            ["com.android.chrome"] = new("Chrome", "Productivity", "Web browser."),
            ["com.google.android.gm"] = new("Gmail", "Productivity", "Email and inbox."),
            ["com.google.android.apps.maps"] = new("Google Maps", "Productivity", "Maps and navigation."),
            ["com.google.android.apps.photos"] = new("Google Photos", "Productivity", "Photo and video library."),
            ["com.google.android.apps.docs"] = new("Google Drive", "Productivity", "Cloud files and documents."),
            ["com.google.android.calendar"] = new("Google Calendar", "Productivity", "Calendar and scheduling."),
            ["com.google.android.keep"] = new("Google Keep", "Productivity", "Notes and reminders."),
            ["com.google.android.apps.messaging"] = new("Google Messages", "Communication", "Text messaging."),
            ["com.google.android.apps.tachyon"] = new("Google Meet", "Communication", "Video meetings."),
            ["com.google.android.apps.authenticator2"] = new("Google Authenticator", "Security", "Two-step verification codes."),
            ["com.google.android.contacts"] = new("Contacts", "Productivity", "Contact management."),
            ["com.google.android.calculator"] = new("Calculator", "Productivity", "Calculator."),
            ["com.google.android.deskclock"] = new("Clock", "System", "Alarms, timer, and clock."),
            ["com.google.android.googlequicksearchbox"] = new("Google", "Productivity", "Search and assistant."),
            ["com.android.vending"] = new("Google Play Store", "Store", "Android app marketplace."),
            ["com.android.settings"] = new("Android Settings", "System", "Android system settings."),
            ["com.android.camera2"] = new("Camera", "System", "Camera capture."),
            ["com.whatsapp"] = new("WhatsApp", "Communication", "Messaging and calls."),
            ["com.discord"] = new("Discord", "Communication", "Community chat and voice."),
            ["com.spotify.music"] = new("Spotify", "Entertainment", "Music and podcasts."),
            ["com.netflix.mediaclient"] = new("Netflix", "Entertainment", "Movies and television."),
            ["com.roblox.client"] = new("Roblox", "Games", "Games and user-created experiences."),
            ["com.supercell.clashofclans"] = new("Clash of Clans", "Games", "Strategy and village-building game."),
            ["com.supercell.clashroyale"] = new("Clash Royale", "Games", "Real-time strategy card game.")
        };

    public static AppMetadata Resolve(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return new AppMetadata("Unknown App", "Other", "Android application.");

        if (KnownPackages.TryGetValue(packageName, out var metadata))
            return metadata;

        var suffix = packageName.Split('.').LastOrDefault() ?? packageName;
        var humanized = Regex.Replace(suffix, "([a-z0-9])([A-Z])", "$1 $2");
        humanized = Regex.Replace(humanized, "[._-]+", " ");
        humanized = Regex.Replace(humanized, "\\s+", " ").Trim();
        var name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(humanized.ToLowerInvariant());

        return new AppMetadata(
            string.IsNullOrWhiteSpace(name) ? packageName : name,
            "Other",
            $"Installed Android app ({packageName}).");
    }
}
