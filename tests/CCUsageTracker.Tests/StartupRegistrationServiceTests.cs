using CCUsageTracker.Startup;
using Microsoft.Win32;

namespace CCUsageTracker.Tests;

public sealed class StartupRegistrationServiceTests : IDisposable
{
    private readonly string _testKeyPath = $@"Software\CCUsageTracker\Tests\{Guid.NewGuid():N}";

    [Fact]
    public void BuildCommandQuotesExecutablePath() =>
        Assert.Equal(
            "\"C:\\Program Files\\CC Usage Tracker\\CCUsageTracker.exe\" --startup",
            StartupRegistrationService.BuildCommand(
                "C:\\Program Files\\CC Usage Tracker\\CCUsageTracker.exe"));

    [Fact]
    public void EnablesAndDisablesStartupInIsolatedRegistryKey()
    {
        Assert.SkipWhen(!OperatingSystem.IsWindows(), "Windows Registry test.");
        const string executable = @"C:\Apps\CC Usage Tracker\CCUsageTracker.exe";
        var service = new StartupRegistrationService(
            executable,
            () => Registry.CurrentUser.CreateSubKey(_testKeyPath, true)!);

        service.SetEnabled(true);
        Assert.True(service.IsEnabled());

        service.SetEnabled(false);
        Assert.False(service.IsEnabled());
    }

    public void Dispose()
    {
        if (OperatingSystem.IsWindows())
            Registry.CurrentUser.DeleteSubKeyTree(_testKeyPath, false);
    }
}
