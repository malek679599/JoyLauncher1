using System;
using System.IO;
using System.Reflection;

namespace JoyLauncher.App.Diagnostics;

internal static class CrashLogWriter
{
    private static readonly object SyncRoot = new();

    public static string? LastLogPath { get; private set; }

    public static string? Write(string source, Exception exception, string? directory = null)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(exception);

            directory ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JoyLauncher",
                "Logs");

            Directory.CreateDirectory(directory);

            var timestamp = DateTimeOffset.Now;
            var fileName = $"crash-{timestamp:yyyyMMdd-HHmmss-fff}-{Environment.ProcessId}.log";
            var path = Path.Combine(directory, fileName);
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

            var contents = string.Join(
                Environment.NewLine,
                "JoyLauncher crash report",
                $"Timestamp (local): {timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}",
                $"Source: {source}",
                $"Application version: {version}",
                $"Process ID: {Environment.ProcessId}",
                $"Windows: {Environment.OSVersion.VersionString}",
                "",
                "Exception details:",
                exception.ToString(),
                "");

            lock (SyncRoot)
            {
                File.AppendAllText(path, contents);
                LastLogPath = path;
            }

            return path;
        }
        catch
        {
            // Crash logging must never throw a second exception while the app is failing.
            return null;
        }
    }
}
