using System;
using System.Runtime.InteropServices;
using System.Text;

namespace JoyLauncher.App.Services;

/// <summary>
/// Reads a process image path with PROCESS_QUERY_LIMITED_INFORMATION, which is
/// less restrictive than reading Process.MainModule for another process.
/// </summary>
internal static class ProcessImagePathReader
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const int MaximumPathLength = 32768;

    public static bool TryGetImagePath(int processId, out string? imagePath, out int win32Error)
    {
        imagePath = null;
        win32Error = 0;

        var processHandle = OpenProcess(ProcessQueryLimitedInformation, false, (uint)processId);
        if (processHandle == IntPtr.Zero)
        {
            win32Error = Marshal.GetLastWin32Error();
            return false;
        }

        try
        {
            var buffer = new StringBuilder(MaximumPathLength);
            uint bufferLength = (uint)buffer.Capacity;

            if (!QueryFullProcessImageName(
                    processHandle,
                    0,
                    buffer,
                    ref bufferLength))
            {
                win32Error = Marshal.GetLastWin32Error();
                return false;
            }

            imagePath = buffer.ToString();
            return !string.IsNullOrWhiteSpace(imagePath);
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint processId);

    [DllImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        IntPtr processHandle,
        uint flags,
        StringBuilder imageFileName,
        ref uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
