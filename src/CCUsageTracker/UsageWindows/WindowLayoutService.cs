using System.Drawing;
using CCUsageTracker.Configuration;

namespace CCUsageTracker.UsageWindows;

public readonly record struct UsageWindowLayout(Rectangle Claude, Rectangle Codex);

public sealed class WindowLayoutService
{
    public UsageWindowLayout Calculate(Rectangle workingArea, uint dpi, AppSettings settings)
    {
        var scale = Math.Max(1, dpi) / 96d;
        var margin = Math.Min((int)Math.Round(settings.OuterMargin * scale), workingArea.Width / 4);
        var gap = Math.Min((int)Math.Round(settings.Gap * scale), workingArea.Width / 4);
        var usableWidth = Math.Max(2, workingArea.Width - (2 * margin));
        var usableHeight = Math.Max(1, workingArea.Height - (2 * margin));
        var combinedWidth = Math.Clamp(
            (int)Math.Round(workingArea.Width * settings.WidthPercent / 100d),
            2,
            usableWidth);
        var height = Math.Clamp(
            (int)Math.Round(workingArea.Height * settings.HeightPercent / 100d),
            1,
            usableHeight);

        gap = Math.Min(gap, Math.Max(0, combinedWidth - 2));
        var contentWidth = combinedWidth - gap;
        var leftWidth = contentWidth / 2;
        var rightWidth = contentWidth - leftWidth;
        var x = workingArea.Left + ((workingArea.Width - combinedWidth) / 2);
        var y = workingArea.Top + ((workingArea.Height - height) / 2);

        return new UsageWindowLayout(
            new Rectangle(x, y, leftWidth, height),
            new Rectangle(x + leftWidth + gap, y, rightWidth, height));
    }
}
