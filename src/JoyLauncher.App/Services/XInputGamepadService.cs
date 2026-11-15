using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace JoyLauncher.App.Services;

public enum GamepadNavigationButton
{
    Up,
    Down,
    Left,
    Right,
    Accept,
    Back,
    Start
}

/// <summary>
/// Reads an Xbox-compatible XInput controller while JoyLauncher is active.
/// It reports button edges only, so a held button does not repeatedly activate controls.
/// </summary>
public sealed class XInputGamepadService : IDisposable
{
    private const uint Up = 0x0001;
    private const uint Down = 0x0002;
    private const uint Left = 0x0004;
    private const uint Right = 0x0008;
    private const uint Back = 0x0020;
    private const uint Start = 0x0010;
    private const uint Accept = 0x1000;
    private const uint Cancel = 0x2000;
    private const uint StickUp = 1u << 16;
    private const uint StickDown = 1u << 17;
    private const uint StickLeft = 1u << 18;
    private const uint StickRight = 1u << 19;
    private const short StickThreshold = 16000;

    private readonly DispatcherTimer _timer;
    private uint _activeController = uint.MaxValue;
    private uint _previousInputs;
    private bool _isConnected;
    private bool _nativeUnavailable;
    private bool _disposed;

    public event Action<GamepadNavigationButton>? ButtonPressed;
    public event Action<bool, string>? ConnectionChanged;

    public bool IsConnected => _isConnected;
    public string StatusText { get; private set; } =
        "No XInput controller detected. Keyboard navigation remains available.";

    public XInputGamepadService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(55)
        };
        _timer.Tick += OnTimerTick;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_timer.IsEnabled && !_nativeUnavailable)
            _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_disposed || _nativeUnavailable)
            return;

        try
        {
            var found = false;
            uint controllerIndex = 0;
            XInputState state = default;

            for (uint index = 0; index < 4; index++)
            {
                if (!TryGetState(index, out state))
                    continue;

                found = true;
                controllerIndex = index;
                break;
            }

            if (!found)
            {
                _activeController = uint.MaxValue;
                _previousInputs = 0;
                SetConnection(false,
                    "No XInput controller detected. Connect an Xbox-compatible controller; keyboard navigation remains available.");
                return;
            }

            if (_activeController != controllerIndex)
            {
                _activeController = controllerIndex;
                _previousInputs = 0;
            }

            SetConnection(true,
                $"XInput controller connected (slot {controllerIndex + 1}). Use the D-pad or left stick to navigate.");

            uint currentInputs = state.Gamepad.Buttons;
            if (state.Gamepad.ThumbLY > StickThreshold) currentInputs |= StickUp;
            if (state.Gamepad.ThumbLY < -StickThreshold) currentInputs |= StickDown;
            if (state.Gamepad.ThumbLX < -StickThreshold) currentInputs |= StickLeft;
            if (state.Gamepad.ThumbLX > StickThreshold) currentInputs |= StickRight;

            var newlyPressed = currentInputs & ~_previousInputs;
            _previousInputs = currentInputs;

            if ((newlyPressed & (Up | StickUp)) != 0) ButtonPressed?.Invoke(GamepadNavigationButton.Up);
            if ((newlyPressed & (Down | StickDown)) != 0) ButtonPressed?.Invoke(GamepadNavigationButton.Down);
            if ((newlyPressed & (Left | StickLeft)) != 0) ButtonPressed?.Invoke(GamepadNavigationButton.Left);
            if ((newlyPressed & (Right | StickRight)) != 0) ButtonPressed?.Invoke(GamepadNavigationButton.Right);
            if ((newlyPressed & Accept) != 0) ButtonPressed?.Invoke(GamepadNavigationButton.Accept);
            if ((newlyPressed & (Back | Cancel)) != 0) ButtonPressed?.Invoke(GamepadNavigationButton.Back);
            if ((newlyPressed & Start) != 0) ButtonPressed?.Invoke(GamepadNavigationButton.Start);
        }
        catch (Exception exception) when (
            exception is DllNotFoundException or
            EntryPointNotFoundException or
            BadImageFormatException)
        {
            _nativeUnavailable = true;
            _timer.Stop();
            SetConnection(false,
                $"Windows XInput is unavailable ({exception.GetType().Name}). Keyboard navigation remains available.");
        }
    }

    private static bool TryGetState(uint userIndex, out XInputState state)
    {
        try
        {
            return XInputGetState14(userIndex, out state) == 0;
        }
        catch (DllNotFoundException)
        {
            return XInputGetStateLegacy(userIndex, out state) == 0;
        }
        catch (EntryPointNotFoundException)
        {
            return XInputGetStateLegacy(userIndex, out state) == 0;
        }
    }

    private void SetConnection(bool connected, string status)
    {
        if (_isConnected == connected && string.Equals(StatusText, status, StringComparison.Ordinal))
            return;

        _isConnected = connected;
        StatusText = status;
        ConnectionChanged?.Invoke(connected, status);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        ButtonPressed = null;
        ConnectionChanged = null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState14(uint userIndex, out XInputState state);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetStateLegacy(uint userIndex, out XInputState state);
}
