using JoyLauncher.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoyLauncher.App.Tests;

[TestClass]
public sealed class LocalPreferencesStoreTests
{
    [TestMethod]
    public void SaveAndLoad_PersistsSortOrderFavoritesPinsAndRecents()
    {
        var directory = Path.Combine(Path.GetTempPath(), "JoyLauncher.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");

        try
        {
            var store = new LocalPreferencesStore(path);
            store.Save(new LocalPreferences
            {
                LibrarySortOrder = AppLibrarySortModes.RecentlyLaunched,
                FavoritePackages = ["com.whatsapp"],
                PinnedPackages = ["com.mojang.minecraftpe"],
                RecentPackages = ["com.whatsapp", "com.android.chrome"]
            });

            var loaded = store.Load();

            Assert.AreEqual(AppLibrarySortModes.RecentlyLaunched, loaded.LibrarySortOrder);
            CollectionAssert.AreEqual(new[] { "com.whatsapp" }, loaded.FavoritePackages);
            CollectionAssert.AreEqual(new[] { "com.mojang.minecraftpe" }, loaded.PinnedPackages);
            CollectionAssert.AreEqual(
                new[] { "com.whatsapp", "com.android.chrome" },
                loaded.RecentPackages);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Load_LegacySettingsWithoutSortOrderUsesNameDefault()
    {
        var directory = Path.Combine(Path.GetTempPath(), "JoyLauncher.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, """{"FavoritePackages":["com.whatsapp"]}""");

        try
        {
            var loaded = new LocalPreferencesStore(path).Load();

            Assert.AreEqual(AppLibrarySortModes.Name, loaded.LibrarySortOrder);
            CollectionAssert.AreEqual(new[] { "com.whatsapp" }, loaded.FavoritePackages);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Save_InvalidSortOrderNormalizesToName()
    {
        var directory = Path.Combine(Path.GetTempPath(), "JoyLauncher.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");

        try
        {
            var store = new LocalPreferencesStore(path);
            store.Save(new LocalPreferences { LibrarySortOrder = "invalid" });

            Assert.AreEqual(AppLibrarySortModes.Name, store.Load().LibrarySortOrder);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
