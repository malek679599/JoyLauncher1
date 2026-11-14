using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace JoyLauncher.App.Services;

public static class BlueStacksWindowLocator
{
    public static IntPtr FindMainPlayerWindow(string? expectedPlayerPath)
    {
        var candidateProcessIds = new HashSet<uint>();

        foreach (var process in Process.GetProcessesByName("HD-Player"))
        {
            using (process)
            {
                try
                {
                    var processPath = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(expectedPlayerPath) &&
                        !string.Equals(
                            Path.GetFullPath(processPath ?? string.Empty),
                            Path.GetFullPath(expectedPlayerPath),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    candidateProcessIds.Add((uint)process.Id);
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException or
                    System.ComponentModel.Win32Exception or
                    NotSupportedException or
                    IOException or
                    UnauthorizedAccessException or
                    ArgumentException)
                {
                    // Skip process metadata that Windows does not expose.
                }
            }
        }

        if (candidateProcessIds.Count == 0)
            return IntPtr.Zero;

        IntPtr bestWindow = IntPtr.Zero;
        long bestArea = 0;

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
