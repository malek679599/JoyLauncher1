namespace JoyLauncher.App.Services;

public static class AppLibrarySortModes
{
    public const string Name = "Name (A–Z)";
    public const string Category = "Category";
    public const string RecentlyLaunched = "Recently launched";
    public const string FavoritesFirst = "Favorites first";
    public const string PinnedFirst = "Pinned first";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        Name,
        RecentlyLaunched,
        FavoritesFirst,
        PinnedFirst,
        Category
    ]);

    public static string Normalize(string? value) =>
        All.FirstOrDefault(option =>
            string.Equals(option, value, StringComparison.OrdinalIgnoreCase)) ?? Name;
}

public sealed record AppLibrarySortItem(
    string Key,
    string Name,
    string Category,
    bool IsFavorite,
    bool IsPinned);

public static class AppLibrarySorter
{
    public static IReadOnlyList<string> Sort(
        IEnumerable<AppLibrarySortItem> items,
        string? sortMode,
        IReadOnlyList<string>? recentPackages)
    {
        ArgumentNullException.ThrowIfNull(items);

        var catalogue = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .ToArray();

        var recentIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (recentPackages is not null)
        {
            for (var index = 0; index < recentPackages.Count; index++)
            {
                var packageName = recentPackages[index];
                if (!string.IsNullOrWhiteSpace(packageName))
                    recentIndices.TryAdd(packageName, index);
            }
        }

        var mode = AppLibrarySortModes.Normalize(sortMode);
        IOrderedEnumerable<AppLibrarySortItem> ordered = mode switch
        {
            AppLibrarySortModes.Category => catalogue
                .OrderBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            AppLibrarySortModes.RecentlyLaunched => catalogue
                .OrderBy(item => recentIndices.TryGetValue(item.Key, out var index) ? index : int.MaxValue)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            AppLibrarySortModes.FavoritesFirst => catalogue
                .OrderByDescending(item => item.IsFavorite)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            AppLibrarySortModes.PinnedFirst => catalogue
                .OrderByDescending(item => item.IsPinned)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            _ => catalogue.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
        };

        return ordered.Select(item => item.Key).ToArray();
    }
}
