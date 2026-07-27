using System.Diagnostics;
using CCUsageTracker.Diagnostics;

namespace CCUsageTracker.Browser;

public sealed class ChromeLauncher(ChromeWindowDetector detector, IAppLogger logger)
{
    public async Task<nint> LaunchAppWindowAsync(
        string chromePath,
        string url,
        CancellationToken cancellationToken)
    {
        var existing = detector.Snapshot();
        var startInfo = new ProcessStartInfo
        {
            FileName = chromePath,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add($"--app={url}");

        logger.Info($"Launching Chrome app window for {url}.");
        using var process = Process.Start(startInfo) ??
                            throw new InvalidOperationException("Chrome did not start.");
        return await detector.WaitForNewWindowAsync(
            existing,
            TimeSpan.FromSeconds(15),
            cancellationToken).ConfigureAwait(false);
    }
}
