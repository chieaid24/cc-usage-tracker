using CCUsageTracker.UsageWindows;

namespace CCUsageTracker.Tests;

public sealed class TrackedWindowRegistryTests
{
    [Fact]
    public void RemoveStaleKeepsOnlyExistingHandles()
    {
        var existingHandles = new HashSet<nint> { new(20) };
        var registry = new TrackedWindowRegistry(existingHandles.Contains);
        registry.Add(new TrackedWindow(new nint(10), "Claude"));
        registry.Add(new TrackedWindow(new nint(20), "Codex"));

        var removed = registry.RemoveStale();

        Assert.Equal(1, removed);
        Assert.Equal([new nint(20)], registry.ExistingHandles());
    }
}
