using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace JoyLauncher.App.Services;

/// <summary>
/// Hosts a separate BlueStacks top-level window inside a WPF surface.
/// This is a best-effort integration: BlueStacks updates or window changes
/// can make external-window hosting unavailable, so callers must keep a fallback.
/// </summary>
public sealed class ExternalWindowHost : HwndHost
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;

    private const long WsChild = 0x40000000L;
    private const long WsPopup = unchecked((long)0x80000000L);
    private const long WsVisible = 0x10000000L;
    private const long WsClipSiblings = 0x04000000L;
    private const long WsClipChildren = 0x02000000L;
    private const long WsOverlappedWindow = 0x00CF0000L;

    private const long WsExAppWindow = 0x00040000L;
    private const long WsExToolWindow = 0x00000080L;

    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;

    private IntPtr _hostHandle;
    private IntPtr _embeddedWindow;
    private IntPtr _originalParent;
    private IntPtr _originalStyle;
    private IntPtr _originalExStyle;
    private bool _hasOriginalState;

    public ExternalWindowHost()
    {
        Focusable = true;
        SizeChanged += (_, _) => ResizeEmbeddedWindow();
    }

    public bool IsAttached => _embeddedWindow != IntPtr.Zero && IsWindow(_embeddedWindow);

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        _hostHandle = CreateWindowEx(
            0,
            "STATIC",
            string.Empty,
            unchecked((int)(WsChild | WsVisible | WsClipChildren | WsClipSiblings)),
            0,
            0,
            Math.Max(1, (int)ActualWidth),
            Math.Max(1, (int)ActualHeight),
            hwndParent.Handle,
            IntPtr.Zero,
            GetModuleHandle(null),
            IntPtr.Zero);

        if (_hostHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the BlueStacks hosting surface.");

        return new HandleRef(this, _hostHandle);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        Detach();
        if (hwnd.Handle != IntPtr.Zero && IsWindow(hwnd.Handle))
            DestroyWindow(hwnd.Handle);
        _hostHandle = IntPtr.Zero;
    }

    public bool TryAttach(IntPtr windowHandle, out string error)
    {
        error = string.Empty;

        if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
        {
            error = "The BlueStacks player window is no longer available.";
            return false;
        }

        if (_hostHandle == IntPtr.Zero || !IsWindow(_hostHandle))
        {
            error = "The in-launcher game surface is not ready. Retry after the Play page appears.";
            return false;
        }

        if (_embeddedWindow == windowHandle && IsAttached)
        {
            ResizeEmbeddedWindow();
            return true;
        }

        Detach();

        _embeddedWindow = windowHandle;
        _originalParent = GetParent(windowHandle);
        _originalStyle = GetWindowLongPtr(windowHandle, GwlStyle);
        _originalExStyle = GetWindowLongPtr(windowHandle, GwlExStyle);
        _hasOriginalState = true;

        var childStyle = (_originalStyle.ToInt64() & ~(WsPopup | WsOverlappedWindow)) |
                         WsChild | WsVisible | WsClipSiblings;

        var childExStyle = (_originalExStyle.ToInt64() & ~WsExAppWindow) | WsExToolWindow;

        SetWindowLongPtr(windowHandle, GwlStyle, new IntPtr(childStyle));
        SetWindowLongPtr(windowHandle, GwlExStyle, new IntPtr(childExStyle));
        SetParent(windowHandle, _hostHandle);

        if (GetParent(windowHandle) != _hostHandle)
        {
            error = "Windows did not allow the BlueStacks window to be attached to JoyLauncher.";
            Detach();
            return false;
        }

        SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            1,
            1,
            SwpNoZOrder | SwpNoActivate | SwpFrameChanged | SwpShowWindow);

        ResizeEmbeddedWindow();
        FocusEmbeddedWindow();
        return true;
    }

    public void FocusEmbeddedWindow()
    {
        if (IsAttached)
        {
            SetFocus(_embeddedWindow);
            SetForegroundWindow(_embeddedWindow);
        }
    }

    public void Detach()
    {
        var window = _embeddedWindow;
        _embeddedWindow = IntPtr.Zero;

        if (!_hasOriginalState || window == IntPtr.Zero || !IsWindow(window))
        {
            _hasOriginalState = false;
            return;
        }

        SetParent(window, _originalParent);
        SetWindowLongPtr(window, GwlStyle, _originalStyle);
        SetWindowLongPtr(window, GwlExStyle, _originalExStyle);
        SetWindowPos(
            window,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoZOrder | SwpNoActivate | SwpFrameChanged | SwpShowWindow | 0x0001 | 0x0002);

        _hasOriginalState = false;
        _originalParent = IntPtr.Zero;
        _originalStyle = IntPtr.Zero;
        _originalExStyle = IntPtr.Zero;
    }

    private void ResizeEmbeddedWindow()
    {
        if (!IsAttached || _hostHandle == IntPtr.Zero)
            return;

        if (!GetClientRect(_hostHandle, out var rect))
            return;

        var width = Math.Max(1, rect.Right - rect.Left);
        var height = Math.Max(1, rect.Bottom - rect.Top);
        SetWindowPos(
            _embeddedWindow,
            IntPtr.Zero,
            0,
            0,
            width,
            height,
            SwpNoZOrder | SwpNoActivate | SwpShowWindow);
    }

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
        int exStyle,
        string className,
        string windowName,
        int style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr parameter);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWnd, IntPtr parent);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int index, int value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out NativeRect rect);

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
