using System.Text;

namespace CCUsageTracker.Diagnostics;

public sealed class RollingFileLogger : IAppLogger
{
    private const long MaxFileBytes = 1024 * 1024;
    private readonly object _gate = new();
    private readonly bool _debugEnabled;
    private StreamWriter? _writer;

    public RollingFileLogger(bool debugEnabled, string? logDirectory = null)
    {
        _debugEnabled = debugEnabled;
        LogDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CCUsageTracker",
            "logs");
    }

    public string LogDirectory { get; }

    public void Info(string message) => Write("INFO", message);

    public void Debug(string message)
    {
        if (_debugEnabled)
            Write("DEBUG", message);
    }

    public void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message} {exception}");

    public void Dispose()
    {
        lock (_gate)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private void Write(string level, string message)
    {
        lock (_gate)
        {
            try
            {
                EnsureWriter();
                _writer!.WriteLine($"{DateTimeOffset.Now:O} [{level}] {Sanitize(message)}");
                _writer.Flush();
            }
            catch
            {
            }
        }
    }

    private void EnsureWriter()
    {
        Directory.CreateDirectory(LogDirectory);
        var currentPath = Path.Combine(LogDirectory, "cc-usage-tracker.log");

        if (_writer is not null && new FileInfo(currentPath).Length < MaxFileBytes)
            return;

        _writer?.Dispose();
        _writer = null;

        if (File.Exists(currentPath) && new FileInfo(currentPath).Length >= MaxFileBytes)
        {
            var oldestPath = Path.Combine(LogDirectory, "cc-usage-tracker.2.log");
            var middlePath = Path.Combine(LogDirectory, "cc-usage-tracker.1.log");
            File.Delete(oldestPath);
            if (File.Exists(middlePath))
                File.Move(middlePath, oldestPath);
            File.Move(currentPath, middlePath);
        }

        _writer = new StreamWriter(
            new FileStream(currentPath, FileMode.Append, FileAccess.Write, FileShare.Read),
            new UTF8Encoding(false));
    }

    private static string Sanitize(string value) =>
        value.Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
