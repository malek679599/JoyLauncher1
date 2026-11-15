using JoyLauncher.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoyLauncher.App.Tests;

[TestClass]
public sealed class AndroidCrashLogParserTests
{
    private const string MinecraftPackage = "com.mojang.minecraftpe";

    [TestMethod]
    public void Extract_IncludesJavaCrashForRequestedPackageAndExcludesOtherPackages()
    {
        const string logcat = """
            11-16 00:00:00.000  100  100 E AndroidRuntime: FATAL EXCEPTION: main
            11-16 00:00:00.001  100  100 E AndroidRuntime: Process: com.mojang.minecraftpe, PID: 100
            11-16 00:00:00.002  100  100 E AndroidRuntime: java.lang.IllegalStateException: Activity failed
            11-16 00:00:00.003  100  100 E AndroidRuntime:     at com.mojang.minecraftpe.MainActivity.onCreate(MainActivity.java:42)
            11-16 00:00:01.000  200  200 E AndroidRuntime: FATAL EXCEPTION: main
            11-16 00:00:01.001  200  200 E AndroidRuntime: Process: com.example.unrelated, PID: 200
            11-16 00:00:01.002  200  200 E AndroidRuntime: java.lang.RuntimeException: unrelated failure
            """;

        var summary = AndroidCrashLogParser.Extract(logcat, MinecraftPackage);

        Assert.IsTrue(summary.CrashFound);
        StringAssert.Contains(summary.Text, "FATAL EXCEPTION");
        StringAssert.Contains(summary.Text, MinecraftPackage);
        StringAssert.Contains(summary.Text, "IllegalStateException");
        Assert.IsFalse(summary.Text.Contains("com.example.unrelated", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Extract_IncludesNativeCrashWhenDebugLogNamesRequestedPackage()
    {
        const string logcat = """
            11-16 00:00:00.000  300  300 F libc    : Fatal signal 11 (SIGSEGV), code 1
            11-16 00:00:00.010  301  301 I DEBUG   : Cmdline: com.mojang.minecraftpe
            11-16 00:00:00.020  301  301 I DEBUG   : #00 pc 0000000000123456 libminecraft.so
            """;

        var summary = AndroidCrashLogParser.Extract(logcat, MinecraftPackage);

        Assert.IsTrue(summary.CrashFound);
        StringAssert.Contains(summary.Text, "Fatal signal 11");
        StringAssert.Contains(summary.Text, MinecraftPackage);
        StringAssert.Contains(summary.Text, "libminecraft.so");
    }

    [TestMethod]
    public void Extract_ReportsWhenNoMatchingCrashWasCaptured()
    {
        const string logcat = """
            11-16 00:00:00.000  100  100 I ActivityManager: Start proc com.mojang.minecraftpe
            11-16 00:00:01.000  100  100 I ActivityManager: Displayed com.mojang.minecraftpe/.MainActivity
            """;

        var summary = AndroidCrashLogParser.Extract(logcat, MinecraftPackage);

        Assert.IsFalse(summary.CrashFound);
        StringAssert.Contains(summary.Text, "No recent Android crash entry");
        StringAssert.Contains(summary.Text, MinecraftPackage);
    }

    [TestMethod]
    public void Extract_StopsAtConfiguredCharacterLimit()
    {
        var lines = new List<string>
        {
            "11-16 00:00:00.000  100  100 E AndroidRuntime: FATAL EXCEPTION: main",
            "11-16 00:00:00.001  100  100 E AndroidRuntime: Process: com.mojang.minecraftpe, PID: 100"
        };
        for (var index = 0; index < 100; index++)
            lines.Add($"11-16 00:00:{index:00}.000  100  100 E AndroidRuntime: stack-frame-{index:D3}");

        var summary = AndroidCrashLogParser.Extract(
            string.Join(Environment.NewLine, lines),
            MinecraftPackage,
            maxCharacters: 256);

        Assert.IsTrue(summary.Text.Length <= 256, $"Expected at most 256 characters, got {summary.Text.Length}.");
        StringAssert.Contains(summary.Text, "[summary truncated]");
    }
}
