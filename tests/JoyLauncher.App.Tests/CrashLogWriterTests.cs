using JoyLauncher.App.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoyLauncher.App.Tests;

[TestClass]
public sealed class CrashLogWriterTests
{
    [TestMethod]
    public void Write_CreatesLocalFileWithSourceAndExceptionDetails()
    {
        var directory = Path.Combine(Path.GetTempPath(), "JoyLauncher.Tests", Guid.NewGuid().ToString("N"));
        var exception = new InvalidOperationException("test failure detail");

        try
        {
            var logPath = CrashLogWriter.Write("WPF Dispatcher", exception, directory);

            Assert.IsNotNull(logPath);
            Assert.IsTrue(File.Exists(logPath), $"Expected crash log at {logPath}");
            var contents = File.ReadAllText(logPath);
            StringAssert.Contains(contents, "WPF Dispatcher");
            StringAssert.Contains(contents, "InvalidOperationException");
            StringAssert.Contains(contents, "test failure detail");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Write_ReturnsNullWhenLogDirectoryCannotBeCreated()
    {
        var parent = Path.Combine(Path.GetTempPath(), "JoyLauncher.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);
        var fileInsteadOfDirectory = Path.Combine(parent, "not-a-directory");
        File.WriteAllText(fileInsteadOfDirectory, "block directory creation");

        try
        {
            var result = CrashLogWriter.Write(
                "WPF Dispatcher",
                new InvalidOperationException("test"),
                Path.Combine(fileInsteadOfDirectory, "logs"));

            Assert.IsNull(result, "Crash logging must never throw when the log path is unavailable.");
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }
}
