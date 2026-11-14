using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace JoyLauncher.App.Services;

public static class BlueStacksWindowLocator
{
    public static IntPtr FindMainPlayerWindow(string? expectedPlayerPath) =>
        FindMainPlayerWindow(expectedPlayerPath, out _);

    public static IntPtr FindMainPlayerWindow(string? expectedPlayerPath, out string diagnostic)
    {
        var candidateProcessIds = new HashSet<uint>();
        var matchingProcesses = 0;
        var inaccessibleProcesses = 0;
        var unverifiedPathProcesses = 0;
        var pathMismatches = 0;
        var pathReadFailures = new List<string>();

        foreach (var process in Process.GetProcessesByName("HD-Player"))
        {
            using (process)
            {
                int processId;
                try
                {
                    processId = process.Id;
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException or
                    System.ComponentModel.Win32Exception or
                    NotSupportedException)
                {
                    inaccessibleProcesses++;
                    continue;
                }

                // GetProcessesByName has already matched the executable name. Do not
                // discard a valid PID just because MainModule is access-denied.
                if (!string.IsNullOrWhiteSpace(expectedPlayerPath))
                {
                    string? processPath = null;
                    var pathError = 0;
                    var pathRead = ProcessImagePathReader.TryGetImagePath(
                        processId, out processPath, out pathError);

                    if (!pathRead)
                    {
                        try
                        {
                            processPath = process.MainModule?.FileName;
                        }
                        catch (Exception exception) when (
                            exception is InvalidOperationException or
                            System.ComponentModel.Win32Exception or
                            NotSupportedException or
                            IOException or
                            UnauthorizedAccessException)
                        {
                            // The PID remains a candidate; path verification is best-effort.
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(processPath))
                    {
                        try
                        {
                            if (!string.Equals(
                                    Path.GetFullPath(processPath),
                                    Path.GetFullPath(expectedPlayerPath),
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                pathMismatches++;
                                continue;
                            }
                        }
                        catch (Exception exception) when (
                            exception is ArgumentException or
                            IOException or
                            UnauthorizedAccessException or
                            NotSupportedException)
                        {
                            unverifiedPathProcesses++;
                            inaccessibleProcesses++;
                            pathReadFailures.Add($"PID {processId}: {exception.GetType().Name}");
                        }
                    }
                    else
                    {
                        // A process returned by GetProcessesByName is still a useful
                        // window-owner candidate even when Windows restricts image-path access.
                        unverifiedPathProcesses++;
                        inaccessibleProcesses++;
                        pathReadFailures.Add($"PID {processId}: Win32 {pathError}");
                    }
                }

                candidateProcessIds.Add((uint)processId);
                matchingProcesses++;
            }
        }

        if (candidateProcessIds.Count == 0)
        {
            diagnostic = $"No HD-Player process candidate was available. Matching processes: {matchingProcesses}; " +
                         $"inaccessible process details: {inaccessibleProcesses}; executable-path mismatches: {pathMismatches}. " +
                         $"Expected path: {expectedPlayerPath ?? "not specified"}.";
            return IntPtr.Zero;
        }

        IntPtr bestWindow = IntPtr.Zero;
        long bestArea = 0;
        uint bestProcessId = 0;
        var visibleWindowCount = 0;

        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window))
                return true;

            GetWindowThreadProcessId(window, out var processId);
            if (!candidateProcessIds.Contains(processId) ||
                !GetWindowRect(window, out var bounds))
            {
                return true;
            }

            visibleWindowCount++;
            var width = Math.Max(0L, (long)bounds.Right - bounds.Left);
            var height = Math.Max(0L, (long)bounds.Bottom - bounds.Top);
            var area = width * height;
            if (area > bestArea)
            {
                bestArea = area;
                bestWindow = window;
                bestProcessId = processId;
            }

            return true;
        }, IntPtr.Zero);

        var processIdList = string.Join(", ", candidateProcessIds);
        var pathDetail = pathReadFailures.Count == 0
            ? string.Empty
            : $" Path verification unavailable: {string.Join("; ", pathReadFailures)}.";

        if (bestWindow == IntPtr.Zero)
        {
            diagnostic = $"Matching HD-Player processes: {matchingProcesses}; candidate PIDs: {processIdList}; " +
                         $"visible top-level windows: {visibleWindowCount}; inaccessible process details: {inaccessibleProcesses}; " +
                         $"path-unverified processes: {unverifiedPathProcesses}; executable-path mismatches: {pathMismatches}. " +
                         $"No visible top-level window owned by those PIDs was found. Expected path: {expectedPlayerPath ?? "not specified"}." +
                         pathDetail;
        }
        else
        {
            diagnostic = $"Matching HD-Player processes: {matchingProcesses}; candidate PIDs: {processIdList}; " +
                         $"visible top-level windows: {visibleWindowCount}; selected HWND: 0x{bestWindow.ToInt64():X}; " +
                         $"selected PID: {bestProcessId}; selected window area: {bestArea} pixels; " +
                         $"inaccessible process details: {inaccessibleProcesses}; path-unverified processes: {unverifiedPathProcesses}; " +
                         $"executable-path mismatches: {pathMismatches}." + pathDetail;
        }

        return bestWindow;
    }

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect bounds);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
