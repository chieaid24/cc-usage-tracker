using System.Reflection;

namespace CCUsageTracker.UI;

public sealed class AboutForm : Form
{
    public AboutForm(Icon icon)
    {
        Text = "About CC Usage Tracker";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(440, 250);

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
        var picture = new PictureBox
        {
            Image = icon.ToBitmap(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(80, 80),
            Location = new Point(20, 20)
        };
        var text = new Label
        {
            AutoSize = false,
            Location = new Point(120, 20),
            Size = new Size(295, 170),
            Text = $"CC Usage Tracker {version}{Environment.NewLine}{Environment.NewLine}" +
                   "Open Claude and Codex usage pages from the Windows system tray." +
                   $"{Environment.NewLine}{Environment.NewLine}" +
                   "This unofficial utility is not affiliated with, endorsed by, or sponsored by Anthropic or OpenAI."
        };
        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Location = new Point(340, 205)
        };
        Controls.AddRange([picture, text, close]);
        AcceptButton = close;
        CancelButton = close;
    }
}
