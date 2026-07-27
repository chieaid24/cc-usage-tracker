using System.Runtime.InteropServices;
using CCUsageTracker.Diagnostics;
using CCUsageTracker.Native;

namespace CCUsageTracker.Hotkeys;

public sealed class EscapeHookService(IAppLogger logger) : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _callback = KeyboardCallback;
    private static EscapeHookService? _activeInstance;
    private nint _hook;
    private Func<IReadOnlyCollection<nint>>? _trackedHandles;
    private Action? _escapePressed;

    public void Install(Func<IReadOnlyCollection<nint>> trackedHandles, Action escapePressed)
    {
        if (_hook != nint.Zero)
            return;

        _trackedHandles = trackedHandles;
        _escapePressed = escapePressed;
        _activeInstance = this;
        _hook = NativeMethods.SetWindowsHookEx(
            NativeConstants.WhKeyboardLl,
            _callback,
            nint.Zero,
            0);
        if (_hook == nint.Zero)
        {
            _activeInstance = null;
            throw new InvalidOperationException(
                $"Could not install the Escape key hook. Windows error {Marshal.GetLastWin32Error()}.");
        }

        logger.Debug("Installed scoped Escape key hook.");
    }

    public void Uninstall()
    {
        if (_hook == nint.Zero)
            return;

        if (!NativeMethods.UnhookWindowsHookEx(_hook))
            logger.Error($"Could not remove keyboard hook. Windows error {Marshal.GetLastWin32Error()}.");
        _hook = nint.Zero;
        _trackedHandles = null;
        _escapePressed = null;
        if (ReferenceEquals(_activeInstance, this))
            _activeInstance = null;
        logger.Debug("Removed scoped Escape key hook.");
    }

    public void Dispose() => Uninstall();

    private static nint KeyboardCallback(int code, nint wParam, nint lParam)
    {
        var instance = _activeInstance;
        if (code >= 0 &&
            instance is not null &&
            (wParam == NativeConstants.WmKeyDown || wParam == NativeConstants.WmSysKeyDown))
        {
            var input = Marshal.PtrToStructure<NativeMethods.LowLevelKeyboardInput>(lParam);
            if (input.VirtualKey == NativeConstants.VkEscape)
            {
                var foreground = NativeMethods.GetForegroundWindow();
                if (instance._trackedHandles?.Invoke().Contains(foreground) == true)
                {
                    instance._escapePressed?.Invoke();
                    return new nint(1);
                }
            }
        }

        return NativeMethods.CallNextHookEx(instance?._hook ?? nint.Zero, code, wParam, lParam);
    }
}
