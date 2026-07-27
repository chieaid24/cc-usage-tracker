using Microsoft.Win32;

namespace CCUsageTracker.Startup;

public sealed class StartupRegistrationService : IStartupRegistrationService
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "CCUsageTracker";
    private readonly Func<RegistryKey> _openRunKey;
    private readonly string _executablePath;

    public StartupRegistrationService(string executablePath, Func<RegistryKey>? openRunKey = null)
    {
        _executablePath = executablePath;
        _openRunKey = openRunKey ?? (() =>
            Registry.CurrentUser.CreateSubKey(RunKeyPath, true) ??
            throw new InvalidOperationException("Could not open the current-user startup registry key."));
    }

    public static string BuildCommand(string executablePath) => $"\"{executablePath}\" --startup";

    public bool IsEnabled()
    {
        using var key = _openRunKey();
        return string.Equals(
            key.GetValue(ValueName) as string,
            BuildCommand(_executablePath),
            StringComparison.Ordinal);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = _openRunKey();
        if (enabled)
            key.SetValue(ValueName, BuildCommand(_executablePath), RegistryValueKind.String);
        else
            key.DeleteValue(ValueName, false);
    }
}
