using JoyLauncher.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoyLauncher.App.Tests;

[TestClass]
public sealed class AppMetadataCatalogTests
{
    [TestMethod]
    public void Resolve_MapsCommonAndroidPackagesToFriendlyMetadata()
    {
        var metadata = AppMetadataCatalog.Resolve("com.whatsapp");

        Assert.AreEqual("WhatsApp", metadata.Name);
        Assert.AreEqual("Communication", metadata.Category);
        Assert.IsFalse(string.IsNullOrWhiteSpace(metadata.Description));
    }

    [TestMethod]
    public void Resolve_HumanizesUnknownPackageSuffix()
    {
        var metadata = AppMetadataCatalog.Resolve("com.example.myCoolApp_client");

        Assert.AreEqual("My Cool App Client", metadata.Name);
        Assert.AreEqual("Other", metadata.Category);
        StringAssert.Contains(metadata.Description, "com.example.myCoolApp_client");
    }
}
