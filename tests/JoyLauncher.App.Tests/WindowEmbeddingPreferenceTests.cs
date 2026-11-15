using JoyLauncher.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoyLauncher.App.Tests;

[TestClass]
public sealed class WindowEmbeddingPreferenceTests
{
    [TestMethod]
    public void NewPreferences_DisableExperimentalEmbeddingByDefault()
    {
        var preferences = new LocalPreferences();

        Assert.IsFalse(preferences.EnableExperimentalEmbedding);
    }

    [TestMethod]
    public void SaveAndLoad_PersistsExplicitExperimentalEmbeddingOptIn()
    {
        var directory = Path.Combine(Path.GetTempPath(), "JoyLauncher.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");

        try
        {
            var store = new LocalPreferencesStore(path);
            store.Save(new LocalPreferences { EnableExperimentalEmbedding = true });

            Assert.IsTrue(store.Load().EnableExperimentalEmbedding);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Load_LegacySettingsWithoutEmbeddingFlagKeepsSafeStandaloneMode()
    {
        var directory = Path.Combine(Path.GetTempPath(), "JoyLauncher.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, """{"FavoritePackages":["com.mojang.minecraftpe"]}""");

        try
        {
            Assert.IsFalse(new LocalPreferencesStore(path).Load().EnableExperimentalEmbedding);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
