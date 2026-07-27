using System.Text.Json;

namespace CCUsageTracker.Configuration;

public sealed class SettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsStore(string? settingsPath = null)
    {
        SettingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CCUsageTracker",
            "settings.json");
    }

    public string SettingsPath { get; }

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return AppSettings.CreateDefault();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null || settings.SchemaVersion != 1 || settings.Validate().Count != 0)
                throw new InvalidDataException("Settings failed validation.");
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException)
        {
            BackupCorruptFile();
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        var errors = settings.Validate();
        if (errors.Count != 0)
            throw new ValidationException(string.Join(Environment.NewLine, errors));

        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".settings.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, SettingsPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private void BackupCorruptFile()
    {
        if (!File.Exists(SettingsPath))
            return;

        var backupPath = $"{SettingsPath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        try
        {
            File.Move(SettingsPath, backupPath, false);
        }
        catch (IOException)
        {
        }
    }
}

public sealed class ValidationException(string message) : Exception(message);
