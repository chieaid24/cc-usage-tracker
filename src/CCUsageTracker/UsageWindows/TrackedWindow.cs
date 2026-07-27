using CCUsageTracker.Native;

namespace CCUsageTracker.UsageWindows;

public sealed class TrackedWindow
{
    public TrackedWindow(nint handle, string provider)
    {
        Handle = handle;
        Provider = provider;
    }

    public nint Handle { get; }
    public string Provider { get; }
    public bool Exists => Handle != nint.Zero && NativeMethods.IsWindow(Handle);
}
