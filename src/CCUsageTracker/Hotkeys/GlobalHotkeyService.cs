using System.ComponentModel;
using System.Runtime.InteropServices;
using CCUsageTracker.Native;

namespace CCUsageTracker.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0x4343;
    private readonly HotkeyWindow _window = new();
    private bool _registered;
    private bool _disposed;

    public event EventHandler? Pressed;

    public void Register()
    {
        if (_registered)
            return;

        _window.HotkeyPressed += OnHotkeyPressed;
        if (!NativeMethods.RegisterHotKey(
                _window.Handle,
                HotkeyId,
                NativeConstants.ModControl | NativeConstants.ModAlt | NativeConstants.ModNoRepeat,
                (uint)Keys.U))
        {
            _window.HotkeyPressed -= OnHotkeyPressed;
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Ctrl+Alt+U is already registered by another application.");
        }

        _registered = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_registered)
        {
            NativeMethods.UnregisterHotKey(_window.Handle, HotkeyId);
            _registered = false;
        }

        _window.HotkeyPressed -= OnHotkeyPressed;
        _window.Dispose();
    }

    private void OnHotkeyPressed(object? sender, EventArgs e) => Pressed?.Invoke(this, EventArgs.Empty);

    private sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        public HotkeyWindow() => CreateHandle(new CreateParams());

        public event EventHandler? HotkeyPressed;

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == NativeConstants.WmHotkey && message.WParam == HotkeyId)
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            base.WndProc(ref message);
        }

        public void Dispose() => DestroyHandle();
    }
}
