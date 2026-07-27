using CCUsageTracker.Configuration;

namespace CCUsageTracker.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"cc-usage-tracker-tests-{Guid.NewGuid():N}");
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    [Fact]
    public void LoadReturnsDefaultsWhenFileDoesNotExist()
    {
        var settings = new SettingsStore(SettingsPath).Load();

        Assert.True(settings.StartWithWindows);
        Assert.Equal("https://claude.ai/settings/usage", settings.ClaudeUsageUrl);
        Assert.Equal("https://chatgpt.com/codex/settings/usage", settings.CodexUsageUrl);
        Assert.Equal(MonitorSelectionMode.Mouse, settings.MonitorSelection);
    }

    [Fact]
    public void SaveAndLoadRoundTrip()
    {
        var store = new SettingsStore(SettingsPath);
        var expected = AppSettings.CreateDefault();
        expected.WidthPercent = 72;
        expected.ChromeExecutablePath = null;
        expected.FirstRunNotificationShown = true;

        store.Save(expected);
        var actual = store.Load();

        Assert.Equal(72, actual.WidthPercent);
        Assert.True(actual.FirstRunNotificationShown);
        Assert.Equal(expected.ClaudeUsageUrl, actual.ClaudeUsageUrl);
    }

    [Fact]
    public void LoadBacksUpCorruptJsonAndReturnsDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsPath, "{ broken");
        var store = new SettingsStore(SettingsPath);

        var settings = store.Load();

        Assert.True(settings.StartWithWindows);
        Assert.False(File.Exists(SettingsPath));
        Assert.Single(Directory.GetFiles(_directory, "settings.json.corrupt-*"));
    }

    [Theory]
    [InlineData("https://example.com/path", true)]
    [InlineData("http://example.com/path", false)]
    [InlineData("relative/path", false)]
    [InlineData("https://", false)]
    [InlineData("", false)]
    public void HttpsUrlValidationMatchesPolicy(string value, bool expected) =>
        Assert.Equal(expected, AppSettings.IsValidHttpsUrl(value));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
