using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

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
        var pathMismatches = 0;

        foreach (var process in Process.GetProcessesByName("HD-Player"))
        {
            using (process)
            {
                try
                {
                    var processPath = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(expectedPlayerPath))
                    {
                        if (string.IsNullOrWhiteSpace(processPath))
                        {
                            inaccessibleProcesses++;
                            continue;
                        }

                        if (!string.Equals(
                                Path.GetFullPath(processPath),
                                Path.GetFullPath(expectedPlayerPath),
                                StringComparison.OrdinalIgnoreCase))
                        {
                            pathMismatches++;
                            continue;
                        }
                    }

                    candidateProcessIds.Add((uint)process.Id);
                    matchingProcesses++;
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException or
                    System.ComponentModel.Win32Exception or
                    NotSupportedException or
                    IOException or
                    UnauthorizedAccessException or
                    ArgumentException)
                {
                    inaccessibleProcesses++;
                }
            }
        }

        if (candidateProcessIds.Count == 0)
        {
            diagnostic = $"No matching HD-Player process. Matching processes: {matchingProcesses}; " +
                         $"inaccessible process details: {inaccessibleProcesses}; executable-path mismatches: {pathMismatches}. " +
                         $"Expected path: {expectedPlayerPath ?? "not specified"}.";
            return IntPtr.Zero;
        }

        IntPtr bestWindow = IntPtr.Zero;
        long bestArea = 0;
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
            }

            return true;
        }, IntPtr.Zero);

        diagnostic = $"Matching HD-Player processes: {matchingProcesses}; visible top-level windows: {visibleWindowCount}; " +
                    $"selected HWND: {(bestWindow == IntPtr.Zero ? "none" : $"0x{bestWindow.ToInt64():X}")}; " +
                    $"selected window area: {bestArea} pixels.";
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
