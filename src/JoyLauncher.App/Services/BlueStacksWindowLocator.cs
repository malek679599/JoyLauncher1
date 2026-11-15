using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace JoyLauncher.App.Services;

public static class BlueStacksWindowLocator
{
    private const uint SnapshotProcesses = 0x00000002;
    private static readonly IntPtr InvalidHandleValue = new(-1);

    public static IntPtr FindMainPlayerWindow(string? expectedPlayerPath) =>
        FindMainPlayerWindow(expectedPlayerPath, out _);

    public static IntPtr FindMainPlayerWindow(string? expectedPlayerPath, out string diagnostic)
    {
        // Toolhelp exposes process IDs and executable names from the process snapshot.
        // Unlike Process.MainModule, this does not require opening every HD-Player
        // process with rights that may be unavailable to a normal desktop process.
        var candidateProcessIds = new HashSet<uint>();
        var matchingProcesses = 0;
        var inaccessibleProcesses = 0;
        var unverifiedPathProcesses = 0;
        var pathMismatches = 0;
        var pathReadFailures = new List<string>();

        var snapshot = CreateToolhelp32Snapshot(SnapshotProcesses, 0);
        if (snapshot == InvalidHandleValue)
        {
            var errorCode = Marshal.GetLastWin32Error();
            diagnostic = $"Windows could not enumerate processes for player discovery: " +
                         (errorCode == 0 ? "unknown Toolhelp snapshot error." :
                             $"Win32 error {errorCode}: {new Win32Exception(errorCode).Message}");
            return IntPtr.Zero;
        }

        try
        {
            var entry = new ProcessEntry32
            {
                Size = (uint)Marshal.SizeOf<ProcessEntry32>()
            };

            var hasEntry = Process32First(snapshot, ref entry);
            while (hasEntry)
            {
                if (string.Equals(entry.ExecutableFile, "HD-Player.exe", StringComparison.OrdinalIgnoreCase))
                {
                    var processId = entry.ProcessId;
                    string? processPath = null;
                    var pathError = 0;

                    if (!string.IsNullOrWhiteSpace(expectedPlayerPath))
                    {
                        var pathRead = ProcessImagePathReader.TryGetImagePath(
                            checked((int)processId), out processPath, out pathError);

                        if (!pathRead || string.IsNullOrWhiteSpace(processPath))
                        {
                            unverifiedPathProcesses++;
                            inaccessibleProcesses++;
                            pathReadFailures.Add($"PID {processId}: Win32 {pathError}");
                        }
                        else
                        {
                            try
                            {
                                if (!string.Equals(
                                        Path.GetFullPath(processPath),
                                        Path.GetFullPath(expectedPlayerPath),
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    pathMismatches++;
                                    hasEntry = Process32Next(snapshot, ref entry);
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
                    }

                    candidateProcessIds.Add(processId);
                    matchingProcesses++;
                }

                hasEntry = Process32Next(snapshot, ref entry);
            }
        }
        finally
        {
            _ = CloseHandle(snapshot);
        }

        if (candidateProcessIds.Count == 0)
        {
            diagnostic = $"No HD-Player process candidate was available. Matching processes: {matchingProcesses}; " +
                         $"inaccessible process details: {inaccessibleProcesses}; executable-path mismatches: {pathMismatches}. " +
                         $"Expected path: {expectedPlayerPath ?? "not specified"}.";
            if (pathReadFailures.Count > 0)
                diagnostic += $" Path verification unavailable: {string.Join("; ", pathReadFailures)}";
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
                return true;

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

        var processIdList = string.Join(", ", candidateProcessIds.OrderBy(id => id));
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public IntPtr DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int BasePriority;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string? ExecutableFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", EntryPoint = "Process32FirstW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", EntryPoint = "Process32NextW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
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
