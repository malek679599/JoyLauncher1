using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace JoyLauncher.App.Services;

public sealed class LocalPreferencesStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsPath;

    public LocalPreferencesStore(string? settingsPath = null)
    {
        _settingsPath = string.IsNullOrWhiteSpace(settingsPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JoyLauncher",
                "settings.json")
            : Path.GetFullPath(settingsPath);
    }

    public LocalPreferences Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new LocalPreferences();

            var json = File.ReadAllText(_settingsPath);
            var preferences = JsonSerializer.Deserialize<LocalPreferences>(json, JsonOptions)
                              ?? new LocalPreferences();
            preferences.Normalize();
            return preferences;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new LocalPreferences();
        }
    }

    public void Save(LocalPreferences preferences)
    {
        preferences.Normalize();
        var directory = Path.GetDirectoryName(_settingsPath)
                        ?? throw new IOException("Could not determine the settings directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = _settingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preferences, JsonOptions));

        if (File.Exists(_settingsPath))
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        else
            File.Move(temporaryPath, _settingsPath);
    }
}

public sealed class LocalPreferences
{
    public List<string> FavoritePackages { get; set; } = [];
    public List<string> PinnedPackages { get; set; } = [];
    public List<string> RecentPackages { get; set; } = [];
    public string LibrarySortOrder { get; set; } = AppLibrarySortModes.Name;

    internal void Normalize()
    {
        LibrarySortOrder = AppLibrarySortModes.Normalize(LibrarySortOrder);
        FavoritePackages = NormalizeList(FavoritePackages);
        PinnedPackages = NormalizeList(PinnedPackages);
        RecentPackages = NormalizeList(RecentPackages).Take(30).ToList();
    }

    private static List<string> NormalizeList(IEnumerable<string>? values) =>
        (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
