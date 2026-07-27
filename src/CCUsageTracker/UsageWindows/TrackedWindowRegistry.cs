using CCUsageTracker.Native;

namespace CCUsageTracker.UsageWindows;

public sealed class TrackedWindowRegistry
{
    private readonly Func<nint, bool> _isWindow;
    private readonly List<TrackedWindow> _windows = [];

    public TrackedWindowRegistry(Func<nint, bool>? isWindow = null) =>
        _isWindow = isWindow ?? NativeMethods.IsWindow;

    public int Count => _windows.Count;

    public void Add(TrackedWindow window) => _windows.Add(window);

    public IReadOnlyList<TrackedWindow> Existing() =>
        _windows.Where(window => _isWindow(window.Handle)).ToArray();

    public IReadOnlyCollection<nint> ExistingHandles() =>
        _windows.Where(window => _isWindow(window.Handle)).Select(window => window.Handle).ToArray();

    public int RemoveStale() => _windows.RemoveAll(window => !_isWindow(window.Handle));

    public void Clear() => _windows.Clear();
}
