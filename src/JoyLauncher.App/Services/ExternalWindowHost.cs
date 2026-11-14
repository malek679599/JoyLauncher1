using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace JoyLauncher.App.Services;

/// <summary>
/// Docks the separate BlueStacks top-level window over the JoyLauncher Play surface.
/// This deliberately avoids SetParent: BlueStacks remains its own process and top-level
/// window, positioned over the Play viewport and owned by the JoyLauncher main window.
/// This is a visual docking technique, not a BlueStacks-supported in-process embed.
/// </summary>
public sealed class ExternalWindowHost : HwndHost
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const int GwlHwndParent = -8;

    private const long WsPopup = unchecked((long)0x80000000L);
    private const long WsChild = 0x40000000L;
    private const long WsVisible = 0x10000000L;
    private const long WsOverlappedWindow = 0x00CF0000L;
    private const long WsClipSiblings = 0x04000000L;
    private const long WsExAppWindow = 0x00040000L;
    private const long WsExToolWindow = 0x00000080L;

    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;
    private const uint SwpNoZOrder = 0x0004;
    private const int SwRestore = 9;
    private static readonly IntPtr HwndTop = IntPtr.Zero;

    private IntPtr _hostHandle;
    private IntPtr _ownerWindow;
    private IntPtr _dockedWindow;
    private IntPtr _originalOwner;
    private IntPtr _originalStyle;
    private IntPtr _originalExStyle;
    private NativeRect _originalBounds;
    private bool _hasOriginalState;

    public ExternalWindowHost()
    {
        Focusable = true;
        SizeChanged += (_, _) =>
        {
            if (IsDocked && !RefreshDockedPosition())
                LastDockingDiagnostic = $"Player was docked, but its position could not be refreshed: {LastWin32Error()}";
        };
    }

    /// <summary>True while the player is still a valid docked top-level window.</summary>
    public bool IsDocked => _dockedWindow != IntPtr.Zero && IsWindow(_dockedWindow);

    // Retain the old property for existing launcher diagnostics/UI bindings.
    public bool IsAttached => IsDocked;

    public IntPtr DockedWindowHandle => IsDocked ? _dockedWindow : IntPtr.Zero;
    public string? LastDockingDiagnostic { get; private set; }
    public string? LastAttachDiagnostic => LastDockingDiagnostic;

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        _ownerWindow = GetAncestor(hwndParent.Handle, 2);
        if (_ownerWindow == IntPtr.Zero)
            _ownerWindow = hwndParent.Handle;

        _hostHandle = CreateWindowEx(
            0,
            "STATIC",
            string.Empty,
            unchecked((int)(WsChild | WsVisible | WsClipSiblings)),
            0,
            0,
            Math.Max(1, (int)ActualWidth),
            Math.Max(1, (int)ActualHeight),
            hwndParent.Handle,
            IntPtr.Zero,
            GetModuleHandle(null),
            IntPtr.Zero);

        if (_hostHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the Play-area docking surface.");

        return new HandleRef(this, _hostHandle);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        Detach();
        if (hwnd.Handle != IntPtr.Zero && IsWindow(hwnd.Handle))
            DestroyWindow(hwnd.Handle);

        _hostHandle = IntPtr.Zero;
        _ownerWindow = IntPtr.Zero;
    }

    /// <summary>
    /// Converts the BlueStacks player into a borderless owned top-level window and
    /// positions it over the native Play surface. No cross-process SetParent call is made.
    /// </summary>
    public bool TryAttach(IntPtr windowHandle, out string error)
    {
        error = string.Empty;
        LastDockingDiagnostic = null;

        if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
            return FailDocking("The selected BlueStacks player window handle is no longer valid.", out error);

        if (_hostHandle == IntPtr.Zero || !IsWindow(_hostHandle))
            return FailDocking("The JoyLauncher Play surface has not created its docking window yet.", out error);

        if (_ownerWindow == IntPtr.Zero || !IsWindow(_ownerWindow))
            return FailDocking("The JoyLauncher main window handle is unavailable.", out error);

        if (_dockedWindow == windowHandle && IsDocked)
            return RefreshDockedPosition(out error);

        Detach();

        _dockedWindow = windowHandle;
        _originalOwner = GetWindowLongPtr(windowHandle, GwlHwndParent);
        _originalStyle = GetWindowLongPtr(windowHandle, GwlStyle);
        _originalExStyle = GetWindowLongPtr(windowHandle, GwlExStyle);

        if (!GetWindowRect(windowHandle, out _originalBounds))
            return FailDocking($"Could not read the original BlueStacks window bounds: {LastWin32Error()}", out error);

        _hasOriginalState = true;

        // Keep it top-level. Strip the normal title bar and resize frame, but do not
        // re-parent it into the WPF HWND tree (which can trigger cross-process DPI resets).
        var popupStyle = (_originalStyle.ToInt64() & ~(WsOverlappedWindow | WsChild)) |
                         WsPopup | WsVisible | WsClipSiblings;
        var popupExStyle = (_originalExStyle.ToInt64() & ~WsExAppWindow) | WsExToolWindow;

        if (!TrySetWindowLongPtr(windowHandle, GwlStyle, new IntPtr(popupStyle), out var styleError))
            return FailDocking($"Could not make the BlueStacks window borderless: {styleError}", out error);

        if (!TrySetWindowLongPtr(windowHandle, GwlExStyle, new IntPtr(popupExStyle), out var exStyleError))
            return FailDocking($"Could not update the BlueStacks window extended style: {exStyleError}", out error);

        // For a top-level popup, GWL_HWNDPARENT sets the owner, not the parent.
        // An owned window stays above its owner and is minimized/restored with it.
        if (!TrySetWindowLongPtr(windowHandle, GwlHwndParent, _ownerWindow, out var ownerError))
            return FailDocking($"Could not associate the BlueStacks window with JoyLauncher: {ownerError}", out error);

        if (IsIconic(windowHandle))
            ShowWindow(windowHandle, SwRestore);

        if (!RefreshDockedPosition(out error))
            return FailDocking(error, out error);

        LastDockingDiagnostic =
            $"Docked top-level HWND 0x{windowHandle.ToInt64():X} over Play surface HWND 0x{_hostHandle.ToInt64():X}. " +
            "BlueStacks remains a separate process; SetParent was not used.";
        FocusDockedWindow();
        return true;
    }

    /// <summary>Repositions the player after JoyLauncher moves, resizes or restores.</summary>
    public bool RefreshDockedPosition()
    {
        if (!IsDocked)
            return false;

        if (!GetWindowRect(_hostHandle, out var bounds))
        {
            LastDockingDiagnostic = $"Could not read the Play-area bounds: {LastWin32Error()}";
            return false;
        }

        var width = Math.Max(1, bounds.Right - bounds.Left);
        var height = Math.Max(1, bounds.Bottom - bounds.Top);
        if (!SetWindowPos(
                _dockedWindow,
                HwndTop,
                bounds.Left,
                bounds.Top,
                width,
                height,
                SwpNoActivate | SwpShowWindow | 0x0200))
        {
            LastDockingDiagnostic = $"SetWindowPos could not dock the player: {LastWin32Error()}";
            return false;
        }

        return true;
    }

    public bool RefreshDockedPosition(out string error)
    {
        var success = RefreshDockedPosition();
        error = success ? string.Empty : LastDockingDiagnostic ?? "The player window is not docked.";
        return success;
    }

    public void FocusDockedWindow()
    {
        if (IsDocked)
        {
            SetForegroundWindow(_dockedWindow);
            SetFocus(_dockedWindow);
        }
    }

    // Retained as an alias for current call sites.
    public void FocusEmbeddedWindow() => FocusDockedWindow();

    public void Detach()
    {
        var window = _dockedWindow;
        _dockedWindow = IntPtr.Zero;

        if (!_hasOriginalState || window == IntPtr.Zero || !IsWindow(window))
        {
            _hasOriginalState = false;
            return;
        }

        // Restore owner and frame before restoring the original desktop position.
        TrySetWindowLongPtr(window, GwlHwndParent, _originalOwner, out _);
        TrySetWindowLongPtr(window, GwlStyle, _originalStyle, out _);
        TrySetWindowLongPtr(window, GwlExStyle, _originalExStyle, out _);

        SetWindowPos(
            window,
            IntPtr.Zero,
            _originalBounds.Left,
            _originalBounds.Top,
            Math.Max(1, _originalBounds.Right - _originalBounds.Left),
            Math.Max(1, _originalBounds.Bottom - _originalBounds.Top),
            SwpNoZOrder | SwpNoActivate | SwpFrameChanged | SwpShowWindow);

        _hasOriginalState = false;
        _originalOwner = IntPtr.Zero;
        _originalStyle = IntPtr.Zero;
        _originalExStyle = IntPtr.Zero;
        LastDockingDiagnostic = "BlueStacks was returned to its original standalone window.";
    }

    private bool FailDocking(string message, out string error)
    {
        error = message;
        LastDockingDiagnostic = message;
        Detach();
        LastDockingDiagnostic = message;
        return false;
    }

    private static bool TrySetWindowLongPtr(IntPtr window, int index, IntPtr value, out string error)
    {
        Marshal.SetLastPInvokeError(0);
        _ = SetWindowLongPtr(window, index, value);
        var lastError = Marshal.GetLastPInvokeError();

        if (lastError != 0)
        {
            error = FormatWin32Error(lastError);
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string LastWin32Error() => FormatWin32Error(Marshal.GetLastWin32Error());

    private static string FormatWin32Error(int errorCode) =>
        errorCode == 0
            ? "Windows did not provide an extended error code."
            : $"Win32 error {errorCode}: {new Win32Exception(errorCode).Message}";

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int index) =>
        IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, index)
            : new IntPtr(GetWindowLong32(hWnd, index));

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, index, value)
            : new IntPtr(SetWindowLong32(hWnd, index, value.ToInt32()));

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int exStyle, string className, string windowName, int style,
        int x, int y, int width, int height, IntPtr parent,
        IntPtr menu, IntPtr instance, IntPtr parameter);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int index, int value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect bounds);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
