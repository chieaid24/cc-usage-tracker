namespace CCUsageTracker.Configuration;

public enum MonitorSelectionMode
{
    Mouse,
    ForegroundWindow,
    Primary
}

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool StartWithWindows { get; set; } = true;
    public string ClaudeUsageUrl { get; set; } = Defaults.ClaudeUsageUrl;
    public string CodexUsageUrl { get; set; } = Defaults.CodexUsageUrl;
    public string? ChromeExecutablePath { get; set; }
    public MonitorSelectionMode MonitorSelection { get; set; } = MonitorSelectionMode.Mouse;
    public int WidthPercent { get; set; } = Defaults.WidthPercent;
    public int HeightPercent { get; set; } = Defaults.HeightPercent;
    public int Gap { get; set; } = Defaults.Gap;
    public int OuterMargin { get; set; } = Defaults.OuterMargin;
    public bool KeepWindowsOnTop { get; set; }
    public bool FirstRunNotificationShown { get; set; }

    public static AppSettings CreateDefault() => new();

    public static bool IsValidHttpsUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(uri.Host);

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (!IsValidHttpsUrl(ClaudeUsageUrl))
            errors.Add("Claude usage URL must be an absolute HTTPS URL.");
        if (!IsValidHttpsUrl(CodexUsageUrl))
            errors.Add("Codex usage URL must be an absolute HTTPS URL.");
        if (ChromeExecutablePath is { Length: > 0 } path &&
            (!Path.IsPathFullyQualified(path) || !File.Exists(path) ||
             !string.Equals(Path.GetFileName(path), "chrome.exe", StringComparison.OrdinalIgnoreCase)))
            errors.Add("Chrome executable path must point to an existing chrome.exe file.");
        if (WidthPercent is < 30 or > 100)
            errors.Add("Width must be between 30 and 100 percent.");
        if (HeightPercent is < 30 or > 100)
            errors.Add("Height must be between 30 and 100 percent.");
        if (Gap is < 0 or > 200)
            errors.Add("Gap must be between 0 and 200 logical pixels.");
        if (OuterMargin is < 0 or > 400)
            errors.Add("Outer margin must be between 0 and 400 logical pixels.");
        if (!Enum.IsDefined(MonitorSelection))
            errors.Add("Monitor selection is invalid.");

        return errors;
    }
}
