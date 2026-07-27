namespace CCUsageTracker.Configuration;

public interface ISettingsStore
{
    string SettingsPath { get; }
    AppSettings Load();
    void Save(AppSettings settings);
}
