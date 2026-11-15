using JoyLauncher.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoyLauncher.App.Tests;

[TestClass]
public sealed class AppLibrarySorterTests
{
    private static readonly AppLibrarySortItem[] Items =
    [
        new("minecraft", "Minecraft", "Games", false, true),
        new("youtube", "YouTube", "Entertainment", true, false),
        new("whatsapp", "WhatsApp", "Communication", false, false),
        new("chrome", "Chrome", "Productivity", true, true)
    ];

    [TestMethod]
    public void Sort_NameUsesCaseInsensitiveAlphabeticalOrder()
    {
        var result = AppLibrarySorter.Sort(Items, AppLibrarySortModes.Name, []);

        CollectionAssert.AreEqual(
            new[] { "chrome", "minecraft", "whatsapp", "youtube" },
            result.ToArray());
    }

    [TestMethod]
    public void Sort_RecentlyLaunchedUsesSavedRecencyThenName()
    {
        var result = AppLibrarySorter.Sort(
            Items,
            AppLibrarySortModes.RecentlyLaunched,
            ["youtube", "minecraft"]);

        CollectionAssert.AreEqual(
            new[] { "youtube", "minecraft", "chrome", "whatsapp" },
            result.ToArray());
    }

    [TestMethod]
    public void Sort_FavoritesFirstUsesNameAsTieBreaker()
    {
        var result = AppLibrarySorter.Sort(Items, AppLibrarySortModes.FavoritesFirst, []);

        CollectionAssert.AreEqual(
            new[] { "chrome", "youtube", "minecraft", "whatsapp" },
            result.ToArray());
    }

    [TestMethod]
    public void Sort_PinnedFirstUsesNameAsTieBreaker()
    {
        var result = AppLibrarySorter.Sort(Items, AppLibrarySortModes.PinnedFirst, []);

        CollectionAssert.AreEqual(
            new[] { "chrome", "minecraft", "whatsapp", "youtube" },
            result.ToArray());
    }

    [TestMethod]
    public void Sort_CategoryGroupsThenSortsByName()
    {
        var result = AppLibrarySorter.Sort(Items, AppLibrarySortModes.Category, []);

        CollectionAssert.AreEqual(
            new[] { "whatsapp", "youtube", "minecraft", "chrome" },
            result.ToArray());
    }

    [TestMethod]
    public void Sort_UnknownModeFallsBackToName()
    {
        var result = AppLibrarySorter.Sort(Items, "not-a-mode", []);

        CollectionAssert.AreEqual(
            new[] { "chrome", "minecraft", "whatsapp", "youtube" },
            result.ToArray());
    }
}
