using System.Drawing;
using System.Runtime.InteropServices;
using CCUsageTracker.Configuration;
using CCUsageTracker.Native;

namespace CCUsageTracker.UsageWindows;

public sealed class MonitorWorkAreaProvider
{
    public (Rectangle WorkingArea, uint Dpi) Get(MonitorSelectionMode selection)
    {
        var monitor = selection switch
        {
            MonitorSelectionMode.Primary => GetPrimaryMonitor(),
            MonitorSelectionMode.ForegroundWindow => NativeMethods.MonitorFromWindow(
                NativeMethods.GetForegroundWindow(),
                NativeConstants.MonitorDefaultToNearest),
            _ => GetMouseMonitor()
        };

        var info = new NativeMethods.MonitorInfo
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>()
        };
        if (monitor == nint.Zero || !NativeMethods.GetMonitorInfo(monitor, ref info))
            throw new InvalidOperationException("Windows did not return monitor information.");

        var area = info.WorkArea;
        var dpi = 96u;
        try
        {
            if (NativeMethods.GetDpiForMonitor(monitor, 0, out var dpiX, out _) == 0)
                dpi = dpiX;
        }
        catch (DllNotFoundException)
        {
        }

        return (Rectangle.FromLTRB(area.Left, area.Top, area.Right, area.Bottom), dpi);
    }

    private static nint GetMouseMonitor()
    {
        if (!NativeMethods.GetCursorPos(out var point))
            return GetPrimaryMonitor();
        return NativeMethods.MonitorFromPoint(point, NativeConstants.MonitorDefaultToPrimary);
    }

    private static nint GetPrimaryMonitor() =>
        NativeMethods.MonitorFromPoint(default, NativeConstants.MonitorDefaultToPrimary);
}
