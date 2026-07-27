using CCUsageTracker.Browser;
using CCUsageTracker.Configuration;

namespace CCUsageTracker.UI;

public sealed class SettingsForm : Form
{
    private readonly ChromeLocator _chromeLocator;
    private readonly CheckBox _startWithWindows = new() { Text = "Start with Windows", AutoSize = true };
    private readonly TextBox _claudeUrl = new() { Dock = DockStyle.Fill };
    private readonly TextBox _codexUrl = new() { Dock = DockStyle.Fill };
    private readonly TextBox _chromePath = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _monitorSelection = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _width = CreateNumber(30, 100);
    private readonly NumericUpDown _height = CreateNumber(30, 100);
    private readonly NumericUpDown _gap = CreateNumber(0, 200);
    private readonly NumericUpDown _margin = CreateNumber(0, 400);
    private readonly CheckBox _keepOnTop = new() { Text = "Keep usage windows on top", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public SettingsForm(AppSettings settings, ChromeLocator chromeLocator, string? hotkeyError)
    {
        _chromeLocator = chromeLocator;
        Text = "CC Usage Tracker Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(660, 450);

        _monitorSelection.DataSource = Enum.GetValues<MonitorSelectionMode>();
        _startWithWindows.Checked = settings.StartWithWindows;
        _claudeUrl.Text = settings.ClaudeUsageUrl;
        _codexUrl.Text = settings.CodexUsageUrl;
        _chromePath.Text = settings.ChromeExecutablePath ?? string.Empty;
        _monitorSelection.SelectedItem = settings.MonitorSelection;
        _width.Value = settings.WidthPercent;
        _height.Value = settings.HeightPercent;
        _gap.Value = settings.Gap;
        _margin.Value = settings.OuterMargin;
        _keepOnTop.Checked = settings.KeepWindowsOnTop;
        _status.Text = hotkeyError ?? string.Empty;

        Controls.Add(BuildLayout());
        AcceptButton = Controls.Find("saveButton", true).Single() as Button;
        CancelButton = Controls.Find("cancelButton", true).Single() as Button;
    }

    public AppSettings? SavedSettings { get; private set; }

    private Control BuildLayout()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 3,
            RowCount = 12
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddWide(table, _startWithWindows, 0);
        AddRow(table, "Claude usage URL", _claudeUrl, CreateButton("Reset URLs", (_, _) => ResetUrls()), 1);
        AddRow(table, "Codex usage URL", _codexUrl, null, 2);
        AddRow(table, "Chrome executable", _chromePath, BuildChromeButtons(), 3);
        AddRow(table, "Monitor", _monitorSelection, null, 4);
        AddRow(table, "Combined width (%)", _width, null, 5);
        AddRow(table, "Height (%)", _height, null, 6);
        AddRow(table, "Gap (logical px)", _gap, null, 7);
        AddRow(table, "Outer margin (logical px)", _margin, null, 8);
        AddWide(table, _keepOnTop, 9);
        AddWide(table, _status, 10);

        var resetButton = CreateButton("Reset all", (_, _) => ResetAll());
        var saveButton = CreateButton("Save", (_, _) => Save());
        saveButton.Name = "saveButton";
        var cancelButton = CreateButton("Cancel", (_, _) => DialogResult = DialogResult.Cancel);
        cancelButton.Name = "cancelButton";
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(resetButton);
        table.Controls.Add(buttons, 0, 11);
        table.SetColumnSpan(buttons, 3);
        return table;
    }

    private Control BuildChromeButtons()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        panel.Controls.Add(CreateButton("Auto-detect", (_, _) =>
        {
            var detected = _chromeLocator.Locate();
            if (detected is null)
                MessageBox.Show(this, "Google Chrome was not found.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                _chromePath.Text = detected;
        }));
        panel.Controls.Add(CreateButton("Browse...", (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Chrome executable (chrome.exe)|chrome.exe|Executables (*.exe)|*.exe",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                _chromePath.Text = dialog.FileName;
        }));
        return panel;
    }

    private void Save()
    {
        var settings = ReadSettings();
        var errors = settings.Validate();
        if (errors.Count != 0)
        {
            MessageBox.Show(
                this,
                string.Join(Environment.NewLine, errors),
                "Invalid settings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        SavedSettings = settings;
        DialogResult = DialogResult.OK;
    }

    private AppSettings ReadSettings() => new()
    {
        StartWithWindows = _startWithWindows.Checked,
        ClaudeUsageUrl = _claudeUrl.Text.Trim(),
        CodexUsageUrl = _codexUrl.Text.Trim(),
        ChromeExecutablePath = string.IsNullOrWhiteSpace(_chromePath.Text) ? null : _chromePath.Text.Trim(),
        MonitorSelection = (MonitorSelectionMode)_monitorSelection.SelectedItem!,
        WidthPercent = (int)_width.Value,
        HeightPercent = (int)_height.Value,
        Gap = (int)_gap.Value,
        OuterMargin = (int)_margin.Value,
        KeepWindowsOnTop = _keepOnTop.Checked,
        FirstRunNotificationShown = true
    };

    private void ResetUrls()
    {
        _claudeUrl.Text = Defaults.ClaudeUsageUrl;
        _codexUrl.Text = Defaults.CodexUsageUrl;
    }

    private void ResetAll()
    {
        var defaults = AppSettings.CreateDefault();
        _startWithWindows.Checked = defaults.StartWithWindows;
        ResetUrls();
        _chromePath.Clear();
        _monitorSelection.SelectedItem = defaults.MonitorSelection;
        _width.Value = defaults.WidthPercent;
        _height.Value = defaults.HeightPercent;
        _gap.Value = defaults.Gap;
        _margin.Value = defaults.OuterMargin;
        _keepOnTop.Checked = defaults.KeepWindowsOnTop;
    }

    private static NumericUpDown CreateNumber(int minimum, int maximum) => new()
    {
        Minimum = minimum,
        Maximum = maximum,
        Width = 90
    };

    private static Button CreateButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += onClick;
        return button;
    }

    private static void AddRow(TableLayoutPanel table, string label, Control input, Control? action, int row)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        table.Controls.Add(input, 1, row);
        if (action is not null)
            table.Controls.Add(action, 2, row);
    }

    private static void AddWide(TableLayoutPanel table, Control control, int row)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(control, 0, row);
        table.SetColumnSpan(control, 3);
    }
}
