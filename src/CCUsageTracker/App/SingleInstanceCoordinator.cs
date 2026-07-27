namespace CCUsageTracker.App;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private const string MutexName = @"Local\CCUsageTracker-5FC75B5E-F24B-444A-A41A-0C3C97CF6AC2";
    private const string EventName = @"Local\CCUsageTracker-Toggle-5FC75B5E-F24B-444A-A41A-0C3C97CF6AC2";
    private readonly Mutex _mutex;
    private readonly EventWaitHandle? _toggleEvent;
    private readonly RegisteredWaitHandle? _registeredWait;
    private readonly SynchronizationContext? _synchronizationContext;
    public SingleInstanceCoordinator(Action toggleRequested) : this(toggleRequested, MutexName, EventName)
    {
    }

    internal SingleInstanceCoordinator(
        Action toggleRequested,
        string mutexName,
        string eventName)
    {
        _mutex = new Mutex(true, mutexName, out var isFirstInstance);
        IsFirstInstance = isFirstInstance;
        if (!isFirstInstance)
            return;

        _synchronizationContext = SynchronizationContext.Current;
        _toggleEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
        _registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _toggleEvent,
            (_, timedOut) =>
            {
                if (!timedOut)
                    _synchronizationContext?.Post(_ => toggleRequested(), null);
            },
            null,
            Timeout.Infinite,
            false);
    }

    public bool IsFirstInstance { get; }

    public static bool SignalExistingInstance() => SignalExistingInstance(EventName);

    internal static bool SignalExistingInstance(string eventName)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var toggleEvent = EventWaitHandle.OpenExisting(eventName);
                return toggleEvent.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Thread.Sleep(25);
            }
        }

        return false;
    }

    public void Dispose()
    {
        _registeredWait?.Unregister(null);
        _toggleEvent?.Dispose();
        if (IsFirstInstance)
            _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
