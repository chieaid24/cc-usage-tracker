using System.Diagnostics;
using System.Text;
using CCUsageTracker.Diagnostics;
using CCUsageTracker.Native;

namespace CCUsageTracker.Browser;

public sealed class ChromeWindowDetector(IAppLogger logger)
{
    public IReadOnlySet<nint> Snapshot() => EnumerateCandidates().Select(x => x.Handle).ToHashSet();

    public async Task<nint> WaitForNewWindowAsync(
        IReadOnlySet<nint> existing,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = EnumerateCandidates()
                .Where(x => !existing.Contains(x.Handle))
                .OrderByDescending(x => x.PreferredClass)
                .ThenBy(x => x.Handle)
                .FirstOrDefault();

            if (candidate.Handle != nint.Zero)
            {
                logger.Debug($"Detected Chrome window HWND=0x{candidate.Handle:X}.");
                return candidate.Handle;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Chrome opened, but its app window could not be identified.");
    }

    private static IReadOnlyList<Candidate> EnumerateCandidates()
    {
        var candidates = new List<Candidate>();
        NativeMethods.EnumWindows((windowHandle, _) =>
        {
            if (!NativeMethods.IsWindowVisible(windowHandle) ||
                NativeMethods.GetWindow(windowHandle, 4) != nint.Zero ||
                !NativeMethods.GetWindowRect(windowHandle, out var rectangle) ||
                rectangle.Right <= rectangle.Left ||
                rectangle.Bottom <= rectangle.Top)
                return true;

            NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);
            try
            {
                using var process = Process.GetProcessById((int)processId);
                if (!string.Equals(process.ProcessName, "chrome", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return true;
            }

            var className = new StringBuilder(256);
            NativeMethods.GetClassName(windowHandle, className, className.Capacity);
            candidates.Add(new Candidate(
                windowHandle,
                string.Equals(className.ToString(), "Chrome_WidgetWin_1", StringComparison.Ordinal)));
            return true;
        }, nint.Zero);
        return candidates;
    }

    private readonly record struct Candidate(nint Handle, bool PreferredClass);
}
