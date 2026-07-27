using System.Diagnostics;
using CCUsageTracker.Browser;
using CCUsageTracker.Configuration;
using CCUsageTracker.Diagnostics;
using CCUsageTracker.Hotkeys;
using CCUsageTracker.Native;

namespace CCUsageTracker.UsageWindows;

public sealed class UsagePopupCoordinator : IDisposable
{
    private readonly ChromeLocator _chromeLocator;
    private readonly ChromeLauncher _chromeLauncher;
    private readonly MonitorWorkAreaProvider _monitorProvider;
    private readonly WindowLayoutService _layoutService;
    private readonly WindowStyleService _styleService;
    private readonly EscapeHookService _escapeHook;
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly TrackedWindowRegistry _windows = new();
    private CancellationTokenSource? _launchCancellation;
    private bool _disposed;

    public UsagePopupCoordinator(
        ChromeLocator chromeLocator,
        ChromeLauncher chromeLauncher,
        MonitorWorkAreaProvider monitorProvider,
        WindowLayoutService layoutService,
        WindowStyleService styleService,
        EscapeHookService escapeHook,
        IAppLogger logger)
    {
        _chromeLocator = chromeLocator;
        _chromeLauncher = chromeLauncher;
        _monitorProvider = monitorProvider;
        _layoutService = layoutService;
        _styleService = styleService;
        _escapeHook = escapeHook;
        _logger = logger;
    }

    public event EventHandler? StateChanged;

    public AppSettings Settings { get; set; } = AppSettings.CreateDefault();

    public bool HasAnyWindows
    {
        get
        {
            RemoveStaleHandles();
            return _windows.Count != 0;
        }
    }

    public bool HasBothWindows
    {
        get
        {
            RemoveStaleHandles();
            return _windows.Count == 2;
        }
    }

    public async Task ToggleAsync()
    {
        if (HasAnyWindows)
            await CloseAsync();
        else
            await OpenAsync();
    }

    public async Task OpenAsync()
    {
        await _operationGate.WaitAsync();
        try
        {
            ThrowIfDisposed();
            RemoveStaleHandles();
            if (_windows.Count != 0)
                return;

            var chromePath = _chromeLocator.Locate(Settings.ChromeExecutablePath) ??
                             throw new ChromeNotFoundException();
            var monitor = _monitorProvider.Get(Settings.MonitorSelection);
            var layout = _layoutService.Calculate(monitor.WorkingArea, monitor.Dpi, Settings);
            _launchCancellation = new CancellationTokenSource();

            try
            {
                var claudeHandle = await _chromeLauncher.LaunchAppWindowAsync(
                    chromePath,
                    Settings.ClaudeUsageUrl,
                    _launchCancellation.Token);
                _windows.Add(new TrackedWindow(claudeHandle, "Claude"));
                _styleService.Apply(claudeHandle, layout.Claude, Settings.KeepWindowsOnTop);

                var codexHandle = await _chromeLauncher.LaunchAppWindowAsync(
                    chromePath,
                    Settings.CodexUsageUrl,
                    _launchCancellation.Token);
                _windows.Add(new TrackedWindow(codexHandle, "Codex"));
                _styleService.Apply(codexHandle, layout.Codex, Settings.KeepWindowsOnTop);

                _escapeHook.Install(CurrentHandles, () => _ = CloseAsync());
                _logger.Info("Opened Claude and Codex usage windows.");
            }
            catch
            {
                await CloseTrackedWindowsCoreAsync();
                throw;
            }
            finally
            {
                _launchCancellation.Dispose();
                _launchCancellation = null;
            }
        }
        finally
        {
            _operationGate.Release();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task CloseAsync()
    {
        _launchCancellation?.Cancel();
        await _operationGate.WaitAsync();
        try
        {
            await CloseTrackedWindowsCoreAsync();
        }
        finally
        {
            _operationGate.Release();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task RefreshAsync()
    {
        await CloseAsync();
        await OpenAsync();
    }

    public void OpenFullPages()
    {
        OpenUrl(Settings.ClaudeUsageUrl);
        OpenUrl(Settings.CodexUsageUrl);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _launchCancellation?.Cancel();
        _escapeHook.Dispose();
        _operationGate.Dispose();
    }

    public void CloseForExit()
    {
        _launchCancellation?.Cancel();
        _escapeHook.Uninstall();
        foreach (var window in _windows.Existing())
        {
            NativeMethods.PostMessage(
                window.Handle,
                NativeConstants.WmClose,
                nint.Zero,
                nint.Zero);
        }

        _windows.Clear();
    }

    private async Task CloseTrackedWindowsCoreAsync()
    {
        _escapeHook.Uninstall();
        var existing = _windows.Existing();
        foreach (var window in existing)
        {
            if (!NativeMethods.PostMessage(
                    window.Handle,
                    NativeConstants.WmClose,
                    nint.Zero,
                    nint.Zero))
                _logger.Error($"Could not post WM_CLOSE to {window.Provider} HWND=0x{window.Handle:X}.");
        }

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (existing.Any(window => window.Exists) && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        _windows.Clear();
        _logger.Info("Closed tracked usage windows.");
    }

    private IReadOnlyCollection<nint> CurrentHandles() =>
        _windows.ExistingHandles();

    private void RemoveStaleHandles()
    {
        var removed = _windows.RemoveStale();
        if (removed == 0)
            return;

        _logger.Info($"Removed {removed} stale usage window handle(s).");
        if (_windows.Count == 0)
            _escapeHook.Uninstall();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void OpenUrl(string url)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        };
        Process.Start(startInfo);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

public sealed class ChromeNotFoundException()
    : Exception("Google Chrome was not found. Select chrome.exe in Settings.");
