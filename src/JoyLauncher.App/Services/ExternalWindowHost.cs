using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace JoyLauncher.App.Services;

/// <summary>
/// Hosts the external BlueStacks player HWND as a child of the native Play surface.
/// Because this crosses process boundaries, attachment is attempted only when Windows
/// reports matching DPI-awareness contexts. A mismatch is refused to avoid Windows
/// forcibly resetting BlueStacks' process DPI-awareness mode.
/// </summary>
public sealed class ExternalWindowHost : HwndHost
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const int GwlHwndParent = -8;
    private const int GwOwner = 4;
    private const uint GaParent = 1;

    private const long WsPopup = unchecked((long)0x80000000L);
    private const long WsChild = 0x40000000L;
    private const long WsVisible = 0x10000000L;
    private const long WsOverlappedWindow = 0x00CF0000L;
    private const long WsClipSiblings = 0x04000000L;
    private const long WsClipChildren = 0x02000000L;

    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoSendChanging = 0x0200;
    private const int SwRestore = 9;

    private IntPtr _hostHandle;
    private IntPtr _ownerWindow;
    private IntPtr _embeddedWindow;
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
                LastDockingDiagnostic = $"Embedded player resize failed: {LastWin32Error()}";
        };
    }

    /// <summary>True only while the player is a child of the JoyLauncher Play surface.</summary>
    public bool IsDocked =>
        _embeddedWindow != IntPtr.Zero &&
        IsWindow(_embeddedWindow) &&
        GetParent(_embeddedWindow) == _hostHandle;

    public bool IsAttached => IsDocked;
    public IntPtr DockedWindowHandle => IsDocked ? _embeddedWindow : IntPtr.Zero;
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
            unchecked((int)(WsChild | WsVisible | WsClipSiblings | WsClipChildren)),
            0,
            0,
            Math.Max(1, (int)ActualWidth),
            Math.Max(1, (int)ActualHeight),
            hwndParent.Handle,
            IntPtr.Zero,
            GetModuleHandle(null),
            IntPtr.Zero);

        if (_hostHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the Play-area child-window surface.");

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
    /// Converts the BlueStacks top-level player window into a child of the Play surface.
    /// The DPI-awareness preflight is deliberately performed before any window mutation.
    /// </summary>
    public bool TryAttach(IntPtr windowHandle, out string error)
    {
        error = string.Empty;
        LastDockingDiagnostic = null;

        if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
            return FailDocking("The selected BlueStacks player HWND is no longer valid.", out error);

        if (_hostHandle == IntPtr.Zero || !IsWindow(_hostHandle))
            return FailDocking("The JoyLauncher Play surface has not created its native child window yet.", out error);

        if (_ownerWindow == IntPtr.Zero || !IsWindow(_ownerWindow))
            return FailDocking("The JoyLauncher main window handle is unavailable.", out error);

        if (_embeddedWindow == windowHandle && IsDocked)
            return RefreshDockedPosition(out error);

        Detach();

        var originalStyle = GetWindowLongPtr(windowHandle, GwlStyle);
        if ((originalStyle.ToInt64() & WsChild) != 0)
            return FailDocking("The selected BlueStacks window is already a child window; a top-level player window was expected.", out error);

        // Cross-process SetParent can forcibly reset the child process DPI mode on
        // Windows 10 1703+ when the awareness contexts differ. Refuse before mutation.
        IntPtr playerDpiContext;
        IntPtr hostDpiContext;
        try
        {
            playerDpiContext = GetWindowDpiAwarenessContext(windowHandle);
            hostDpiContext = GetWindowDpiAwarenessContext(_hostHandle);
            if (playerDpiContext == IntPtr.Zero || hostDpiContext == IntPtr.Zero)
                return FailDocking("Windows could not report both DPI-awareness contexts. BlueStacks was left standalone.", out error);

            if (!AreDpiAwarenessContextsEqual(playerDpiContext, hostDpiContext))
            {
                var playerDpi = GetDpiForWindow(windowHandle);
                var hostDpi = GetDpiForWindow(_hostHandle);
                return FailDocking(
                    $"DPI-awareness mismatch: BlueStacks context 0x{playerDpiContext.ToInt64():X} at {playerDpi} DPI; " +
                    $"JoyLauncher context 0x{hostDpiContext.ToInt64():X} at {hostDpi} DPI. " +
                    "Windows can forcibly reset the BlueStacks process DPI mode during cross-process SetParent, " +
                    "so this build safely refused to embed it. BlueStacks was not re-parented.",
                    out error);
            }
        }
        catch (EntryPointNotFoundException)
        {
            return FailDocking("This Windows build does not expose the DPI-awareness APIs required for a safe child-window attachment. BlueStacks was left standalone.", out error);
        }

        _embeddedWindow = windowHandle;
        _originalOwner = GetWindow(windowHandle, GwOwner);
        _originalStyle = originalStyle;
        _originalExStyle = GetWindowLongPtr(windowHandle, GwlExStyle);

        if (!GetWindowRect(windowHandle, out _originalBounds))
            return FailDocking($"Could not capture the original BlueStacks window bounds: {LastWin32Error()}", out error);

        _hasOriginalState = true;

        // Microsoft documents that SetParent does not alter WS_CHILD / WS_POPUP.
        // Set the child style first, then establish the native parent relationship.
        var childStyle = (_originalStyle.ToInt64() & ~(WsOverlappedWindow | WsPopup)) |
                         WsChild | WsVisible | WsClipSiblings | WsClipChildren;
        if (!TrySetWindowLongPtr(windowHandle, GwlStyle, new IntPtr(childStyle), out var styleError))
            return FailDocking($"Could not switch BlueStacks to a child-window style: {styleError}", out error);

        Marshal.SetLastPInvokeError(0);
        var previousParent = SetParent(windowHandle, _hostHandle);
        var parentError = Marshal.GetLastPInvokeError();
        if (previousParent == IntPtr.Zero && parentError != 0)
            return FailDocking($"Windows SetParent failed: {FormatWin32Error(parentError)}", out error);

        if (GetParent(windowHandle) != _hostHandle)
            return FailDocking("SetParent returned without establishing the expected JoyLauncher parent HWND.", out error);

        if (IsIconic(windowHandle))
            ShowWindow(windowHandle, SwRestore);

        if (!RefreshDockedPosition(out error))
        {
            var resizeError = error;
            return FailDocking(resizeError, out error);
        }

        var playerProcessId = GetWindowThreadProcessId(windowHandle, out var targetProcessId);
        var playerDpi = GetDpiForWindow(windowHandle);
        var hostDpi = GetDpiForWindow(_hostHandle);
        LastDockingDiagnostic =
            $"Child-window hosting succeeded: player HWND 0x{windowHandle.ToInt64():X}, " +
            $"parent HWND 0x{_hostHandle.ToInt64():X}, BlueStacks PID {targetProcessId}, " +
            $"DPI contexts matched ({playerDpi} DPI / {hostDpi} DPI). " +
            "BlueStacks remains a separate process; the native window is parented to the Play surface.";
        FocusDockedWindow();
        return true;
    }

    /// <summary>Resizes the child player to exactly fill the native Play surface.</summary>
    public bool RefreshDockedPosition()
    {
        if (!IsDocked)
            return false;

        if (!GetClientRect(_hostHandle, out var bounds))
        {
            LastDockingDiagnostic = $"Could not read the Play-area client bounds: {LastWin32Error()}";
            return false;
        }

        var width = Math.Max(1, bounds.Right - bounds.Left);
        var height = Math.Max(1, bounds.Bottom - bounds.Top);
        if (!SetWindowPos(
                _embeddedWindow,
                IntPtr.Zero,
                0,
                0,
                width,
                height,
                SwpNoZOrder | SwpNoActivate | SwpFrameChanged | SwpShowWindow | SwpNoSendChanging))
        {
            LastDockingDiagnostic = $"SetWindowPos could not size the embedded player: {LastWin32Error()}";
            return false;
        }

        return true;
    }

    public bool RefreshDockedPosition(out string error)
    {
        var success = RefreshDockedPosition();
        error = success ? string.Empty : LastDockingDiagnostic ?? "The BlueStacks player is not embedded.";
        return success;
    }

    public void FocusDockedWindow()
    {
        if (IsDocked)
        {
            SetForegroundWindow(_ownerWindow);
            SetFocus(_embeddedWindow);
        }
    }

    public void FocusEmbeddedWindow() => FocusDockedWindow();

    /// <summary>
    /// Returns the BlueStacks player to a normal desktop top-level window and restores
    /// the exact style, extended style, owner, and screen bounds captured before attach.
    /// </summary>
    public void Detach()
    {
        var window = _embeddedWindow;
        if (!_hasOriginalState || window == IntPtr.Zero || !IsWindow(window))
        {
            _embeddedWindow = IntPtr.Zero;
            _hasOriginalState = false;
            return;
        }

        Marshal.SetLastPInvokeError(0);
        _ = SetParent(window, IntPtr.Zero);
        var detachError = Marshal.GetLastPInvokeError();
        if (detachError != 0)
        {
            LastDockingDiagnostic = $"Could not return BlueStacks to the desktop parent: {FormatWin32Error(detachError)}";
            return;
        }

        var styleRestored = TrySetWindowLongPtr(window, GwlStyle, _originalStyle, out var styleError);
        var exStyleRestored = TrySetWindowLongPtr(window, GwlExStyle, _originalExStyle, out var exStyleError);
        var ownerRestored = TrySetWindowLongPtr(window, GwlHwndParent, _originalOwner, out var ownerError);

        if (styleRestored && exStyleRestored && ownerRestored)
        {
            SetWindowPos(
                window,
                IntPtr.Zero,
                _originalBounds.Left,
                _originalBounds.Top,
                Math.Max(1, _originalBounds.Right - _originalBounds.Left),
                Math.Max(1, _originalBounds.Bottom - _originalBounds.Top),
                SwpNoZOrder | SwpNoActivate | SwpFrameChanged | SwpShowWindow);
        }

        _embeddedWindow = IntPtr.Zero;
        _hasOriginalState = false;
        _originalOwner = IntPtr.Zero;
        _originalStyle = IntPtr.Zero;
        _originalExStyle = IntPtr.Zero;

        LastDockingDiagnostic = styleRestored && exStyleRestored && ownerRestored
            ? "BlueStacks was detached and its original standalone window styles, owner, and bounds were restored."
            : $"BlueStacks was detached, but restoration reported: {styleError} {exStyleError} {ownerError}".Trim();
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

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int index, int value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect bounds);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out NativeRect bounds);

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

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
