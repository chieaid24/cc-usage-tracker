using CCUsageTracker.Configuration;
using CCUsageTracker.Native;
using CCUsageTracker.UsageWindows;

namespace CCUsageTracker.Tests;

public sealed class NativeInteropTests
{
    [Fact]
    public void MonitorAndMessageEntryPointsResolveOnWindows()
    {
        Assert.SkipWhen(!OperatingSystem.IsWindows(), "Win32 entry-point test.");

        var monitor = new MonitorWorkAreaProvider().Get(MonitorSelectionMode.Primary);
        NativeMethods.PostMessage(nint.Zero, 0, nint.Zero, nint.Zero);

        Assert.True(monitor.WorkingArea.Width > 0);
        Assert.True(monitor.WorkingArea.Height > 0);
    }
}
