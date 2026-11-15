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

    public bool TryFocus(IntPtr targetWindow, IntPtr ownerWindow, out string diagnostic)
    {
        diagnostic = string.Empty;
        if (targetWindow == IntPtr.Zero)
        {
            diagnostic = "The BlueStacks window handle is null.";
            return false;
        }

        var currentThreadId = _api.GetCurrentThreadId();
        var targetThreadId = _api.GetWindowThreadId(targetWindow);
        if (currentThreadId == 0 || targetThreadId == 0)
        {
            diagnostic =
                $"Windows could not identify both input threads (JoyLauncher {currentThreadId}, BlueStacks {targetThreadId}); focus was not changed.";
            return false;
        }

        var inputQueuesAttached = false;
        if (currentThreadId != targetThreadId)
        {
            if (!_api.AttachThreadInput(currentThreadId, targetThreadId, attach: true))
            {
                var error = _api.GetLastError();
                diagnostic =
                    $"Windows AttachThreadInput failed while connecting JoyLauncher thread {currentThreadId} to BlueStacks thread {targetThreadId}: " +
                    FormatWin32Error(error);
                return false;
            }

            inputQueuesAttached = true;
        }

        var focusMoved = false;
        var inputQueuesDetached = true;
        var detachError = 0;

        try
        {
            if (ownerWindow != IntPtr.Zero)
                _ = _api.SetForegroundWindow(ownerWindow);

            _ = _api.SetFocus(targetWindow);
            focusMoved = _api.GetFocus() == targetWindow;
        }
        finally
        {
            if (inputQueuesAttached &&
                !_api.AttachThreadInput(currentThreadId, targetThreadId, attach: false))
            {
                inputQueuesDetached = false;
                detachError = _api.GetLastError();
            }
        }

        if (!inputQueuesDetached)
        {
            diagnostic =
                $"Keyboard focus {(focusMoved ? "moved" : "did not move")} to BlueStacks, but Windows could not detach the temporarily shared input queues: " +
                FormatWin32Error(detachError);
            return false;
        }

        if (!focusMoved)
        {
            diagnostic =
                "Windows did not move keyboard focus to the hosted BlueStacks window after its input queue was made available.";
            return false;
        }

        diagnostic = currentThreadId == targetThreadId
            ? "Keyboard focus moved to the hosted BlueStacks window on the current UI thread."
            : "Keyboard focus moved to the hosted BlueStacks window; the cross-thread input queues were detached immediately afterward.";
        return true;
    }

    private static string FormatWin32Error(int errorCode) =>
        errorCode == 0
            ? "Windows did not provide an extended error code."
            : $"Win32 error {errorCode}: {new System.ComponentModel.Win32Exception(errorCode).Message}";
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
