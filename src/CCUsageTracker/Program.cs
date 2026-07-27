using CCUsageTracker.App;
using CCUsageTracker.Configuration;
using CCUsageTracker.Diagnostics;
using CCUsageTracker.Startup;

namespace CCUsageTracker;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        TrayApplicationContext? applicationContext = null;
        using var singleInstance = new SingleInstanceCoordinator(
            () => applicationContext?.ToggleFromSecondInstance());
        if (!singleInstance.IsFirstInstance)
        {
            SingleInstanceCoordinator.SignalExistingInstance();
            return;
        }

        var debugEnabled = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
        var logger = new RollingFileLogger(debugEnabled);
        var settingsStore = new SettingsStore();
        if (args.Contains("--no-startup", StringComparer.OrdinalIgnoreCase))
        {
            var settings = settingsStore.Load();
            settings.StartWithWindows = false;
            settingsStore.Save(settings);
        }
        var startup = new StartupRegistrationService(Environment.ProcessPath ??
            throw new InvalidOperationException("The executable path is unavailable."));
        applicationContext = new TrayApplicationContext(settingsStore, startup, logger);
        Application.Run(applicationContext);
    }
}
