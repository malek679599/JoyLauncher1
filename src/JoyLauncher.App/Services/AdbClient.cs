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

        return AdbScanResult.Succeeded(serial, packageNames);
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

public sealed record AdbScanResult(
    bool Success,
    string? DeviceSerial,
    IReadOnlyList<string> PackageNames,
    string? Error)
{
    public static AdbScanResult Succeeded(string serial, IReadOnlyList<string> packages) =>
        new(true, serial, packages, null);

    public static AdbScanResult Failed(string error) =>
        new(false, null, Array.Empty<string>(), error);
}
