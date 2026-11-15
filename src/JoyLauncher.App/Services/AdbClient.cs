using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JoyLauncher.App.Services;

public sealed class AdbClient
{
    public async Task<AdbScanResult> ConnectAndListPackagesAsync(
        string adbExecutable,
        BlueStacksAdbEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(adbExecutable))
            return AdbScanResult.Failed("The discovered ADB executable no longer exists.");

        var address = $"{endpoint.Host}:{endpoint.Port}";
        var connect = await RunAsync(
            adbExecutable,
            ["connect", address],
            TimeSpan.FromSeconds(12),
            cancellationToken);

        if (connect.TimedOut)
            return AdbScanResult.Failed("ADB connection timed out. Check that BlueStacks is running and ADB is enabled.");

        var devices = await RunAsync(
            adbExecutable,
            ["devices", "-l"],
            TimeSpan.FromSeconds(12),
            cancellationToken);

        if (devices.TimedOut)
            return AdbScanResult.Failed("ADB did not return its device list before the timeout.");

        if (devices.ExitCode != 0)
            return AdbScanResult.Failed(FormatError("Could not read the ADB device list.", devices));

        var serial = FindReadyDeviceSerial(devices.StandardOutput, address);
        if (serial is null)
        {
            var detail = string.IsNullOrWhiteSpace(connect.StandardError)
                ? connect.StandardOutput.Trim()
                : connect.StandardError.Trim();
            return AdbScanResult.Failed(
                string.IsNullOrWhiteSpace(detail)
                    ? $"BlueStacks did not appear as a ready ADB device at {address}."
                    : $"BlueStacks is not ready at {address}. {detail}");
        }

        var packages = await RunAsync(
            adbExecutable,
            ["-s", serial, "shell", "pm", "list", "packages"],
            TimeSpan.FromSeconds(20),
            cancellationToken);

        if (packages.TimedOut)
            return AdbScanResult.Failed("The Android package query timed out.");

        if (packages.ExitCode != 0)
            return AdbScanResult.Failed(FormatError("Could not list Android packages.", packages));

        var packageNames = packages.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("package:", StringComparison.OrdinalIgnoreCase))
            .Select(line => line["package:".Length..].Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var launcherActivities = await RunAsync(
            adbExecutable,
            [
                "-s", serial,
                "shell", "cmd", "package", "query-activities",
                "--components",
                "-a", "android.intent.action.MAIN",
                "-c", "android.intent.category.LAUNCHER"
            ],
            TimeSpan.FromSeconds(15),
            cancellationToken);

        var installedPackageSet = new HashSet<string>(
            packageNames,
            StringComparer.OrdinalIgnoreCase);

        var launchablePackageNames = ParseLauncherPackageNames(
            launcherActivities.StandardOutput,
            installedPackageSet);

        var launchabilityDetectionAvailable =
            !launcherActivities.TimedOut &&
            launcherActivities.ExitCode == 0 &&
            (launchablePackageNames.Count > 0 ||
             launcherActivities.StandardOutput.Contains(
                 "No activities found",
                 StringComparison.OrdinalIgnoreCase));

        var launchabilityDetectionError = launchabilityDetectionAvailable
            ? null
            : launcherActivities.TimedOut
                ? "Android launcher-activity discovery timed out. The full installed-package list is shown as a compatibility fallback."
                : FormatError(
                    "Android launcher-activity discovery is unavailable. The full installed-package list is shown as a compatibility fallback.",
                    launcherActivities);

        // Older Android package-manager shells may not support query-activities.
        // Keep the library usable, but clearly mark launchability as unverified.
        if (!launchabilityDetectionAvailable)
            launchablePackageNames = packageNames;

        return AdbScanResult.Succeeded(
            serial,
            packageNames,
            launchablePackageNames,
            launchabilityDetectionAvailable,
            launchabilityDetectionError);
    }

    public async Task<AdbCommandResult> LaunchPackageAsync(
        string adbExecutable,
        string deviceSerial,
        string packageName,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(adbExecutable))
            return AdbCommandResult.Failed("The discovered ADB executable no longer exists.");

        if (string.IsNullOrWhiteSpace(deviceSerial))
            return AdbCommandResult.Failed("No verified ADB device is selected.");

        if (!IsValidPackageName(packageName))
            return AdbCommandResult.Failed("The selected package name is not valid.");

        return await RunAsync(
            adbExecutable,
            ["-s", deviceSerial, "shell", "monkey", "-p", packageName, "-c", "android.intent.category.LAUNCHER", "1"],
            TimeSpan.FromSeconds(15),
            cancellationToken);
    }

    public async Task<AdbCrashSummaryResult> GetRecentCrashSummaryAsync(
        string adbExecutable,
        string deviceSerial,
        string packageName,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(adbExecutable))
            return AdbCrashSummaryResult.Failed("The discovered ADB executable no longer exists.");

        if (string.IsNullOrWhiteSpace(deviceSerial))
            return AdbCrashSummaryResult.Failed("No verified ADB device is selected.");

        if (!IsValidPackageName(packageName))
            return AdbCrashSummaryResult.Failed("The selected package name is not valid.");

        // This runs only after an explicit user action. It reads a bounded recent window,
        // never clears logcat, and returns only crash blocks matching the selected package.
        var logcat = await RunAsync(
            adbExecutable,
            ["-s", deviceSerial, "logcat", "-d", "-v", "threadtime", "-t", "700"],
            TimeSpan.FromSeconds(15),
            cancellationToken);

        if (logcat.TimedOut)
            return AdbCrashSummaryResult.Failed("Android logcat did not return within 15 seconds.");

        if (logcat.ExitCode != 0)
            return AdbCrashSummaryResult.Failed(FormatError("Could not read Android crash logs.", logcat));

        var summary = AndroidCrashLogParser.Extract(logcat.StandardOutput, packageName);
        return AdbCrashSummaryResult.Succeeded(summary.Text, summary.CrashFound);
    }

    private static IReadOnlyList<string> ParseLauncherPackageNames(
        string output,
        ISet<string> installedPackages)
    {
        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();

            // Some package-manager builds wrap the flattened component in ComponentInfo{...}.
            var openingBrace = line.IndexOf('{');
            var closingBrace = line.LastIndexOf('}');
            if (openingBrace >= 0 && closingBrace > openingBrace)
                line = line[(openingBrace + 1)..closingBrace].Trim();

            var separator = line.IndexOf('/');
            if (separator <= 0)
                continue;

            var packageName = line[..separator].Trim();
            if (IsValidPackageName(packageName) && installedPackages.Contains(packageName))
                packages.Add(packageName);
        }

        return packages
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsValidPackageName(string packageName)
    {
        var segments = packageName.Split('.');
        return segments.Length >= 2 &&
               segments.All(segment =>
                   segment.Length > 0 &&
                   segment.All(character => char.IsLetterOrDigit(character) || character == '_'));
    }

    private static string? FindReadyDeviceSerial(string output, string expectedAddress)
    {
        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !string.Equals(parts[1], "device", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(parts[0], expectedAddress, StringComparison.OrdinalIgnoreCase))
                return parts[0];
        }

        return null;
    }

    private static string FormatError(string summary, AdbCommandResult result)
    {
        var detail = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput.Trim()
            : result.StandardError.Trim();

        return string.IsNullOrWhiteSpace(detail) ? summary : $"{summary} {detail}";
    }

    private static async Task<AdbCommandResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        try
        {
            if (!process.Start())
                return AdbCommandResult.Failed("ADB process could not be started.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between the check and the kill.
                }

                try
                {
                    await process.WaitForExitAsync(CancellationToken.None);
                    await Task.WhenAll(stdoutTask, stderrTask);
                }
                catch (InvalidOperationException)
                {
                    // The process may already have been disposed by the operating system.
                }

                if (cancellationToken.IsCancellationRequested)
                    throw;

                return AdbCommandResult.Timeout();
            }

            await Task.WhenAll(stdoutTask, stderrTask);
            return new AdbCommandResult(
                process.ExitCode,
                stdoutTask.Result,
                stderrTask.Result,
                TimedOut: false,
                StartError: null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            System.ComponentModel.Win32Exception or
            IOException or
            UnauthorizedAccessException)
        {
            return AdbCommandResult.Failed(exception.Message);
        }
    }
}

public sealed record AdbCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut,
    string? StartError)
{
    public static AdbCommandResult Failed(string message) =>
        new(-1, string.Empty, message, false, message);

    public static AdbCommandResult Timeout() =>
        new(-1, string.Empty, "Command timed out.", true, null);
}

public sealed record AdbCrashSummaryResult(
    bool Success,
    string Summary,
    bool CrashFound,
    string? Error)
{
    public static AdbCrashSummaryResult Succeeded(string summary, bool crashFound) =>
        new(true, summary, crashFound, null);

    public static AdbCrashSummaryResult Failed(string error) =>
        new(false, string.Empty, false, error);
}

public sealed record AdbScanResult(
    bool Success,
    string? DeviceSerial,
    IReadOnlyList<string> PackageNames,
    IReadOnlyList<string> LaunchablePackageNames,
    bool LaunchabilityDetectionAvailable,
    string? LaunchabilityDetectionError,
    string? Error)
{
    public static AdbScanResult Succeeded(
        string serial,
        IReadOnlyList<string> packages,
        IReadOnlyList<string> launchablePackages,
        bool launchabilityDetectionAvailable,
        string? launchabilityDetectionError) =>
        new(
            true,
            serial,
            packages,
            launchablePackages,
            launchabilityDetectionAvailable,
            launchabilityDetectionError,
            null);

    public static AdbScanResult Failed(string error) =>
        new(
            false,
            null,
            Array.Empty<string>(),
            Array.Empty<string>(),
            false,
            null,
            error);
}
