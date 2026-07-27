using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using CCUsageTracker.Diagnostics;
using CCUsageTracker.Native;

namespace CCUsageTracker.UsageWindows;

public sealed class WindowStyleService(IAppLogger logger)
{
    public void Apply(nint windowHandle, Rectangle rectangle, bool topmost)
    {
        if (!NativeMethods.IsWindow(windowHandle))
            throw new InvalidOperationException("The Chrome window no longer exists.");

        Marshal.SetLastPInvokeError(0);
        var currentStyle = NativeMethods.GetWindowLongPtr(windowHandle, NativeConstants.GwlExStyle);
        var error = Marshal.GetLastWin32Error();
        if (currentStyle == nint.Zero && error != 0)
            throw new Win32Exception(error, "Could not read the Chrome window style.");

        var updatedStyle = (currentStyle.ToInt64() | NativeConstants.WsExToolWindow) &
                           ~NativeConstants.WsExAppWindow;
        Marshal.SetLastPInvokeError(0);
        var previousStyle = NativeMethods.SetWindowLongPtr(
            windowHandle,
            NativeConstants.GwlExStyle,
            new nint(updatedStyle));
        error = Marshal.GetLastWin32Error();
        if (previousStyle == nint.Zero && error != 0)
            throw new Win32Exception(error, PrivilegeMessage);

        var insertAfter = topmost ? new nint(-1) : new nint(-2);
        if (!NativeMethods.SetWindowPos(
                windowHandle,
                insertAfter,
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                rectangle.Height,
                NativeConstants.SwpFrameChanged | NativeConstants.SwpShowWindow))
            throw new Win32Exception(Marshal.GetLastWin32Error(), PrivilegeMessage);

        logger.Info(
            $"Styled HWND=0x{windowHandle:X} at {rectangle.X},{rectangle.Y} " +
            $"{rectangle.Width}x{rectangle.Height}; topmost={topmost}.");
    }

    private const string PrivilegeMessage =
        "Windows blocked the Chrome window change. Run Chrome and CC Usage Tracker at the same privilege level.";
}
