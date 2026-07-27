using System.Diagnostics;
using System.Reflection;
using CCUsageTracker.Browser;
using CCUsageTracker.Configuration;
using CCUsageTracker.Diagnostics;
using CCUsageTracker.Hotkeys;
using CCUsageTracker.Startup;
using CCUsageTracker.UI;
using CCUsageTracker.UsageWindows;
using Microsoft.Win32;

namespace CCUsageTracker.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ISettingsStore _settingsStore;
    private readonly IStartupRegistrationService _startupRegistration;
    private readonly IAppLogger _logger;
    private readonly ChromeLocator _chromeLocator;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly UsagePopupCoordinator _popupCoordinator;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _showUsage;
    private readonly ToolStripMenuItem _closeUsage;
    private readonly ToolStripMenuItem _refreshUsage;
    private readonly ToolStripMenuItem _startWithWindows;
    private readonly System.Windows.Forms.Timer _stateTimer;
    private readonly Icon _appIcon;
    private AppSettings _settings;
    private string? _hotkeyError;
    private bool _exitRequested;
    private bool _exiting;

    public TrayApplicationContext(
        ISettingsStore settingsStore,
        IStartupRegistrationService startupRegistration,
        IAppLogger logger)
    {
        _settingsStore = settingsStore;
        _startupRegistration = startupRegistration;
        _logger = logger;
        _settings = settingsStore.Load();
        _chromeLocator = new ChromeLocator();

        var detector = new ChromeWindowDetector(logger);
        var escapeHook = new EscapeHookService(logger);
        _popupCoordinator = new UsagePopupCoordinator(
            _chromeLocator,
            new ChromeLauncher(detector, logger),
            new MonitorWorkAreaProvider(),
            new WindowLayoutService(),
            new WindowStyleService(logger),
            escapeHook,
            logger)
        {
            Settings = _settings
        };
        _popupCoordinator.StateChanged += (_, _) => UpdateMenuState();

        _hotkeyService = new GlobalHotkeyService();
        _hotkeyService.Pressed += (_, _) => RunOperation(_popupCoordinator.ToggleAsync);

        _appIcon = LoadIcon();
        (_trayIcon, _showUsage, _closeUsage, _refreshUsage, _startWithWindows) = BuildTrayIcon();
        _stateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _stateTimer.Tick += (_, _) => UpdateMenuState();
        _stateTimer.Start();

        SystemEvents.SessionEnding += OnSessionEnding;
        Initialize();
    }

    public void ToggleFromSecondInstance() => RunOperation(_popupCoordinator.ToggleAsync);

    protected override void ExitThreadCore()
    {
        if (_exiting)
            return;
        _exiting = true;
        _logger.Info("Shutting down.");

        _stateTimer.Stop();
        SystemEvents.SessionEnding -= OnSessionEnding;
        _hotkeyService.Dispose();
        _popupCoordinator.CloseForExit();
        _popupCoordinator.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _appIcon.Dispose();
        _logger.Dispose();
        base.ExitThreadCore();
    }

    private void Initialize()
    {
        _logger.Info("Starting CC Usage Tracker.");
        try
        {
            _startupRegistration.SetEnabled(_settings.StartWithWindows);
            _startWithWindows.Checked = _startupRegistration.IsEnabled();
        }
        catch (Exception ex)
        {
            _logger.Error("Could not update startup registration.", ex);
            ShowBalloon("Startup setting", "CC Usage Tracker could not update its Windows startup setting.", ToolTipIcon.Warning);
        }

        try
        {
            _hotkeyService.Register();
            _logger.Info("Registered Ctrl+Alt+U.");
        }
        catch (Exception ex)
        {
            _hotkeyError = ex.Message;
            _logger.Error("Could not register Ctrl+Alt+U.", ex);
            ShowBalloon("Hotkey unavailable", ex.Message, ToolTipIcon.Warning);
        }

        if (!_settings.FirstRunNotificationShown)
        {
            ShowBalloon(
                "CC Usage Tracker",
                "CC Usage Tracker is running in the tray. Press Ctrl+Alt+U to view usage.",
                ToolTipIcon.Info);
            _settings.FirstRunNotificationShown = true;
            _settingsStore.Save(_settings);
        }
    }

    private (NotifyIcon, ToolStripMenuItem, ToolStripMenuItem, ToolStripMenuItem, ToolStripMenuItem)
        BuildTrayIcon()
    {
        var menu = new ContextMenuStrip();
        var show = new ToolStripMenuItem("Show Usage", null, (_, _) => RunOperation(_popupCoordinator.OpenAsync));
        var close = new ToolStripMenuItem("Close Usage", null, (_, _) => RunOperation(_popupCoordinator.CloseAsync));
        var refresh = new ToolStripMenuItem("Refresh Usage", null, (_, _) => RunOperation(_popupCoordinator.RefreshAsync));
        var fullPages = new ToolStripMenuItem("Open Full Pages", null, (_, _) => RunAction(_popupCoordinator.OpenFullPages));
        var settings = new ToolStripMenuItem("Settings...", null, (_, _) => ShowSettings());
        var startup = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true
        };
        startup.Click += (_, _) => ToggleStartup(startup.Checked);
        var viewLogs = new ToolStripMenuItem("View Logs", null, (_, _) => OpenLogDirectory());
        var about = new ToolStripMenuItem("About", null, (_, _) =>
        {
            using var form = new AboutForm(_appIcon);
            form.ShowDialog();
        });
        var exit = new ToolStripMenuItem("Exit", null, async (_, _) => await ExitAsync());

        menu.Items.AddRange([
            show,
            close,
            refresh,
            new ToolStripSeparator(),
            fullPages,
            settings,
            new ToolStripSeparator(),
            startup,
            new ToolStripSeparator(),
            viewLogs,
            about,
            exit
        ]);

        var trayIcon = new NotifyIcon
        {
            Text = "CC Usage Tracker",
            Icon = _appIcon,
            ContextMenuStrip = menu,
            Visible = true
        };
        trayIcon.DoubleClick += (_, _) => RunOperation(_popupCoordinator.ToggleAsync);
        return (trayIcon, show, close, refresh, startup);
    }

    private async void RunOperation(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (ChromeNotFoundException ex)
        {
            _logger.Error("Chrome was not found.", ex);
            ShowBalloon("Chrome required", ex.Message, ToolTipIcon.Warning);
            ShowSettings();
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Window operation canceled.");
        }
        catch (Exception ex)
        {
            _logger.Error("Usage window operation failed.", ex);
            ShowBalloon("Could not open usage", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            UpdateMenuState();
        }
    }

    private void RunAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _logger.Error("Action failed.", ex);
            ShowBalloon("CC Usage Tracker", ex.Message, ToolTipIcon.Error);
        }
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings, _chromeLocator, _hotkeyError);
        if (form.ShowDialog() != DialogResult.OK || form.SavedSettings is null)
            return;

        try
        {
            _settings = form.SavedSettings;
            _settingsStore.Save(_settings);
            _startupRegistration.SetEnabled(_settings.StartWithWindows);
            _popupCoordinator.Settings = _settings;
            _startWithWindows.Checked = _startupRegistration.IsEnabled();
            _logger.Info("Saved settings.");
        }
        catch (Exception ex)
        {
            _logger.Error("Could not save settings.", ex);
            MessageBox.Show(
                ex.Message,
                "Could not save settings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ToggleStartup(bool enabled)
    {
        try
        {
            _startupRegistration.SetEnabled(enabled);
            _settings.StartWithWindows = enabled;
            _settingsStore.Save(_settings);
            _startWithWindows.Checked = _startupRegistration.IsEnabled();
        }
        catch (Exception ex)
        {
            _startWithWindows.Checked = !enabled;
            _logger.Error("Could not change startup registration.", ex);
            ShowBalloon("Startup setting", ex.Message, ToolTipIcon.Error);
        }
    }

    private void OpenLogDirectory()
    {
        Directory.CreateDirectory(_logger.LogDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _logger.LogDirectory,
            UseShellExecute = true
        });
    }

    private void UpdateMenuState()
    {
        var hasAny = _popupCoordinator.HasAnyWindows;
        _showUsage.Enabled = !_popupCoordinator.HasBothWindows;
        _closeUsage.Enabled = hasAny;
        _refreshUsage.Enabled = hasAny;
    }

    private void ShowBalloon(string title, string text, ToolTipIcon icon)
    {
        _trayIcon.BalloonTipTitle = title;
        _trayIcon.BalloonTipText = text;
        _trayIcon.BalloonTipIcon = icon;
        _trayIcon.ShowBalloonTip(5000);
    }

    private static Icon LoadIcon()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("CCUsageTracker.Assets.Icon.ico")
            ?? throw new InvalidOperationException("The application icon is missing.");
        return new Icon(stream);
    }

    private async Task ExitAsync()
    {
        if (_exitRequested)
            return;

        _exitRequested = true;
        _hotkeyService.Dispose();
        try
        {
            await _popupCoordinator.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Could not close usage windows during exit.", ex);
            _popupCoordinator.CloseForExit();
        }
        finally
        {
            ExitThread();
        }
    }

    private void OnSessionEnding(object sender, SessionEndingEventArgs e)
    {
        _popupCoordinator.CloseForExit();
        ExitThread();
    }
}
