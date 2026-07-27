using System.Drawing;
using CCUsageTracker.Configuration;
using CCUsageTracker.UsageWindows;

namespace CCUsageTracker.Tests;

public sealed class WindowLayoutServiceTests
{
    private readonly WindowLayoutService _service = new();

    [Fact]
    public void CalculatesDefaultLayoutFor1080pWorkingArea()
    {
        var layout = _service.Calculate(new Rectangle(0, 0, 1920, 1040), 96, AppSettings.CreateDefault());

        Assert.Equal(new Rectangle(134, 104, 819, 832), layout.Claude);
        Assert.Equal(new Rectangle(965, 104, 820, 832), layout.Codex);
    }

    [Fact]
    public void ScalesLogicalSpacingOnHighDpi4kMonitor()
    {
        var layout = _service.Calculate(new Rectangle(0, 0, 3840, 2080), 192, AppSettings.CreateDefault());

        Assert.Equal(269, layout.Claude.Left);
        Assert.Equal(24, layout.Codex.Left - layout.Claude.Right);
        Assert.Equal(1664, layout.Claude.Height);
    }

    [Fact]
    public void PreservesNegativeMonitorCoordinates()
    {
        var area = new Rectangle(-1920, 0, 1920, 1040);
        var layout = _service.Calculate(area, 96, AppSettings.CreateDefault());

        Assert.True(layout.Claude.Left < 0);
        Assert.True(layout.Codex.Right <= area.Right);
        Assert.True(area.Contains(layout.Claude));
        Assert.True(area.Contains(layout.Codex));
    }

    [Fact]
    public void ClampsLayoutToNarrowWorkingArea()
    {
        var area = new Rectangle(100, 50, 320, 700);
        var settings = AppSettings.CreateDefault();
        settings.OuterMargin = 400;
        settings.Gap = 200;

        var layout = _service.Calculate(area, 144, settings);

        Assert.True(layout.Claude.Width >= 1);
        Assert.True(layout.Codex.Width >= 1);
        Assert.True(area.Contains(layout.Claude));
        Assert.True(area.Contains(layout.Codex));
    }

    [Fact]
    public void UsesOnlySuppliedWorkingArea()
    {
        var workingArea = new Rectangle(0, 0, 1920, 1000);

        var layout = _service.Calculate(workingArea, 96, AppSettings.CreateDefault());

        Assert.True(layout.Claude.Bottom <= 1000);
        Assert.True(layout.Codex.Bottom <= 1000);
    }
}
