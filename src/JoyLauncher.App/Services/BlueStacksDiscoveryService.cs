using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace JoyLauncher.App.Services;

public sealed class BlueStacksDiscoveryService
{
    private static readonly string[] RegistryLocations =
    [
        @"SOFTWARE\BlueStacks_nxt",
        @"SOFTWARE\BlueStacks"
    ];

    private static readonly Regex AdbPortPattern = new(
        @"bst\.instance\.(?<instance>[^.\s=]+)\.status\.adb_port\s*=\s*""?(?<port>\d+)""?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public BlueStacksDiscoveryResult Discover()
    {
        var installDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var runningPlayerPaths = FindRunningPlayerPaths();

        foreach (var playerPath in runningPlayerPaths)
            AddDirectory(installDirectories, Path.GetDirectoryName(playerPath));

        foreach (var directory in ReadRegistryInstallDirectories())
            AddDirectory(installDirectories, directory);

        AddDirectory(installDirectories, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "BlueStacks_nxt"));
        AddDirectory(installDirectories, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "BlueStacks_nxt"));

        var validPlayerPaths = installDirectories
            .Select(directory => Path.Combine(directory, "HD-Player.exe"))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var playerPathFound = runningPlayerPaths.FirstOrDefault(File.Exists)
            ?? validPlayerPaths.FirstOrDefault();

        var adbPath = installDirectories
            .SelectMany(directory => new[]
            {
                Path.Combine(directory, "HD-Adb.exe"),
                Path.Combine(directory, "adb.exe")
            })
            .FirstOrDefault(File.Exists);

        var configPaths = FindConfigurationPaths(installDirectories);
        var endpoints = configPaths
            .SelectMany(ParseAdbEndpoints)
            .GroupBy(endpoint => endpoint.Port)
            .Select(group => group.First())
            .OrderBy(endpoint => endpoint.InstanceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new BlueStacksDiscoveryResult(
            PlayerRunning: runningPlayerPaths.Count > 0,
            PlayerPath: playerPathFound,
            AdbPath: adbPath,
            ConfigurationPath: configPaths.FirstOrDefault(),
            AdbEndpoints: endpoints,
            CandidateInstallDirectories: installDirectories.OrderBy(path => path).ToArray());
    }

    private static List<string> FindRunningPlayerPaths()
    {
        var paths = new List<string>();

        foreach (var process in Process.GetProcessesByName("HD-Player"))
        {
            using (process)
            {
                try
                {
                    var processId = process.Id;
                    string? path = null;

                    if (!ProcessImagePathReader.TryGetImagePath(processId, out path, out _))
                        path = process.MainModule?.FileName;

                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                        paths.Add(path);
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException or
                    System.ComponentModel.Win32Exception or
                    NotSupportedException)
                {
                    // Process metadata can be unavailable due to permissions or process exit.
                }
            }
        }

        return paths;
    }

    private static IEnumerable<string> ReadRegistryInstallDirectories()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);

            foreach (var root in new[] { localMachine, currentUser })
            {
                foreach (var location in RegistryLocations)
                {
                    using var key = root.OpenSubKey(location);
                    var value = key?.GetValue("InstallDir") as string;
                    if (!string.IsNullOrWhiteSpace(value))
                        yield return value;
                }
            }

            using var uninstall = localMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall is null)
                continue;

            foreach (var subKeyName in uninstall.GetSubKeyNames())
            {
                using var appKey = uninstall.OpenSubKey(subKeyName);
                var displayName = appKey?.GetValue("DisplayName") as string;
                var installLocation = appKey?.GetValue("InstallLocation") as string;

                if (!string.IsNullOrWhiteSpace(displayName) &&
                    displayName.Contains("BlueStacks", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(installLocation))
                {
                    yield return installLocation;
                }
            }
        }
    }

    private static IReadOnlyList<string> FindConfigurationPaths(IEnumerable<string> installDirectories)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "BlueStacks_nxt",
                "bluestacks.conf")
        };

        foreach (var directory in installDirectories)
            candidates.Add(Path.Combine(directory, "bluestacks.conf"));

        return candidates.Where(File.Exists).ToArray();
    }

    private static IEnumerable<BlueStacksAdbEndpoint> ParseAdbEndpoints(string configPath)
    {
        IEnumerable<string> lines;
        try
        {
            lines = File.ReadLines(configPath).ToArray();
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var line in lines)
        {
            var match = AdbPortPattern.Match(line);
            if (!match.Success ||
                !int.TryParse(match.Groups["port"].Value, out var port) ||
                port is < 1 or > 65535)
                continue;

            yield return new BlueStacksAdbEndpoint(
                match.Groups["instance"].Value,
                "127.0.0.1",
                port,
                configPath);
        }
    }

    private static void AddDirectory(ISet<string> directories, string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return;

        try
        {
            if (Directory.Exists(directory))
                directories.Add(Path.GetFullPath(directory));
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException or
            NotSupportedException)
        {
            // Ignore invalid or inaccessible candidates and keep discovery running.
        }
    }
}

public sealed record BlueStacksAdbEndpoint(
    string InstanceName,
    string Host,
    int Port,
    string SourcePath);

public sealed record BlueStacksDiscoveryResult(
    bool PlayerRunning,
    string? PlayerPath,
    string? AdbPath,
    string? ConfigurationPath,
    IReadOnlyList<BlueStacksAdbEndpoint> AdbEndpoints,
    IReadOnlyList<string> CandidateInstallDirectories)
{
    public bool PlayerFound => !string.IsNullOrWhiteSpace(PlayerPath);
    public bool AdbExecutableFound => !string.IsNullOrWhiteSpace(AdbPath);
    public bool HasConfiguredAdbEndpoint => AdbEndpoints.Count > 0;
}
