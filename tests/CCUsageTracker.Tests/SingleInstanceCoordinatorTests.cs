using CCUsageTracker.App;

namespace CCUsageTracker.Tests;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void SecondLaunchSignalInvokesToggleCallback()
    {
        Assert.SkipWhen(!OperatingSystem.IsWindows(), "Named Windows event test.");
        var signalCount = 0;
        SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());
        using var coordinator = new SingleInstanceCoordinator(() => Interlocked.Increment(ref signalCount));

        Assert.True(coordinator.IsFirstInstance);
        Assert.True(SingleInstanceCoordinator.SignalExistingInstance());
        Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref signalCount) == 1, TimeSpan.FromSeconds(2)));
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state) => callback(state);
    }
}
