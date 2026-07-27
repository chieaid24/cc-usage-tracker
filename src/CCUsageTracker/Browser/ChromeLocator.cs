using Microsoft.Win32;

namespace CCUsageTracker.Browser;

public sealed class ChromeLocator
{
    public string? Locate(string? overridePath = null)
    {
        if (IsChromeExecutable(overridePath))
            return Path.GetFullPath(overridePath!);

        foreach (var path in CandidatePaths())
        {
            if (IsChromeExecutable(path))
                return path;
        }

        return null;
    }

    private static bool IsChromeExecutable(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        Path.IsPathFullyQualified(path) &&
        File.Exists(path) &&
        string.Equals(Path.GetFileName(path), "chrome.exe", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> CandidatePaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        yield return Path.Combine(localAppData, "Google", "Chrome", "Application", "chrome.exe");
        yield return Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe");
        yield return Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe");

        foreach (var registryPath in new[]
                 {
                     @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe",
                     @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe"
                 })
        {
            using var currentUser = Registry.CurrentUser.OpenSubKey(registryPath);
            if (currentUser?.GetValue(null) is string currentUserPath)
                yield return currentUserPath;

            using var localMachine = Registry.LocalMachine.OpenSubKey(registryPath);
            if (localMachine?.GetValue(null) is string machinePath)
                yield return machinePath;
        }
    }
}
