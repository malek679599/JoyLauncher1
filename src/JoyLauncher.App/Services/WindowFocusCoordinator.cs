using System;
using System.Runtime.InteropServices;

namespace JoyLauncher.App.Services;

internal interface IWindowFocusApi
{
    uint GetCurrentThreadId();
    uint GetWindowThreadId(IntPtr window);
    bool AttachThreadInput(uint fromThreadId, uint toThreadId, bool attach);
    bool SetForegroundWindow(IntPtr window);
    IntPtr SetFocus(IntPtr window);
    IntPtr GetFocus();
    int GetLastError();
}

internal sealed class WindowFocusCoordinator
{
    private readonly IWindowFocusApi _api;

    public WindowFocusCoordinator(IWindowFocusApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    // Baseline behavior mirrors the existing direct SetFocus call. Regression tests
    // exercise the required cross-thread queue handling before the implementation is fixed.
    public bool TryFocus(IntPtr targetWindow, IntPtr ownerWindow, out string diagnostic)
    {
        diagnostic = string.Empty;
        if (targetWindow == IntPtr.Zero)
        {
            diagnostic = "The BlueStacks window handle is null.";
            return false;
        }

        if (ownerWindow != IntPtr.Zero)
            _ = _api.SetForegroundWindow(ownerWindow);

        _ = _api.SetFocus(targetWindow);
        if (_api.GetFocus() == targetWindow)
        {
            diagnostic = "Keyboard focus moved to the hosted BlueStacks window.";
            return true;
        }

        diagnostic = "Windows did not move keyboard focus to the hosted BlueStacks window.";
        return false;
    }
}

internal sealed class Win32WindowFocusApi : IWindowFocusApi
{
    public uint GetCurrentThreadId() => GetCurrentThreadIdNative();

    public uint GetWindowThreadId(IntPtr window) =>
        GetWindowThreadProcessId(window, out _);

    public bool AttachThreadInput(uint fromThreadId, uint toThreadId, bool attach) =>
        AttachThreadInputNative(fromThreadId, toThreadId, attach);

    public bool SetForegroundWindow(IntPtr window) => SetForegroundWindowNative(window);

    public IntPtr SetFocus(IntPtr window) => SetFocusNative(window);

    public IntPtr GetFocus() => GetFocusNative();

    public int GetLastError() => Marshal.GetLastWin32Error();

    [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
    private static extern uint GetCurrentThreadIdNative();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInputNative(uint fromThreadId, uint toThreadId, [MarshalAs(UnmanagedType.Bool)] bool attach);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindowNative(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocusNative(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocusNative();
}
