namespace CCUsageTracker.Diagnostics;

public interface IAppLogger : IDisposable
{
    string LogDirectory { get; }
    void Info(string message);
    void Debug(string message);
    void Error(string message, Exception? exception = null);
}
