using Avalonia;
using LightUpUI.Presentation;

namespace LightUpTest.Presentation;

public sealed class TileCornerTriggerPolicyTests
{
    private static readonly PixelRect WorkArea = new(100, 50, 1920, 1080);

    [Theory]
    [InlineData(100, 50, ScreenCorner.TopLeft)]
    [InlineData(2019, 50, ScreenCorner.TopRight)]
    [InlineData(100, 1129, ScreenCorner.BottomLeft)]
    [InlineData(2019, 1129, ScreenCorner.BottomRight)]
    public void GetCorner_detects_each_working_area_corner(int x, int y, ScreenCorner expected)
    {
        Assert.Equal(expected, TileCornerTriggerPolicy.GetCorner(new PixelPoint(x, y), [WorkArea], 8));
    }

    [Fact]
    public void GetCorner_rejects_points_outside_the_hot_zone()
    {
        Assert.Null(TileCornerTriggerPolicy.GetCorner(new PixelPoint(108, 58), [WorkArea], 8));
        Assert.Null(TileCornerTriggerPolicy.GetCorner(new PixelPoint(900, 500), [WorkArea], 8));
    }

    [Fact]
    public void GetCorner_uses_full_screen_bounds_when_the_taskbar_reduces_working_area()
    {
        var screen = new PixelRect(0, 0, 1920, 1080);
        var workingArea = new PixelRect(0, 0, 1920, 1040);

        Assert.Equal(
            ScreenCorner.BottomLeft,
            TileCornerTriggerPolicy.GetCorner(
                new PixelPoint(2, 1078),
                [new TileLauncherScreenArea(screen, workingArea)],
                8));
        Assert.Equal(
            ScreenCorner.BottomRight,
            TileCornerTriggerPolicy.GetCorner(
                new PixelPoint(1918, 1078),
                [new TileLauncherScreenArea(screen, workingArea)],
                8));
    }

    [Fact]
    public void HasDwelled_requires_the_configured_delay()
    {
        var enteredAt = DateTimeOffset.UtcNow;

        Assert.False(TileCornerTriggerPolicy.HasDwelled(enteredAt, enteredAt.AddMilliseconds(699), TimeSpan.FromMilliseconds(700)));
        Assert.True(TileCornerTriggerPolicy.HasDwelled(enteredAt, enteredAt.AddMilliseconds(700), TimeSpan.FromMilliseconds(700)));
        Assert.False(TileCornerTriggerPolicy.HasDwelled(enteredAt, enteredAt.AddMilliseconds(-1), TimeSpan.FromMilliseconds(700)));
    }

    [Fact]
    public void GetWindowPosition_clamps_each_corner_inside_the_working_area()
    {
        var position = TileCornerTriggerPolicy.GetWindowPosition(
            WorkArea,
            new PixelSize(980, 640),
            ScreenCorner.BottomRight,
            margin: 0);

        Assert.Equal(new PixelPoint(1040, 490), position);
        Assert.True(new PixelRect(position, new PixelSize(980, 640)).Right <= WorkArea.Right);
        Assert.True(new PixelRect(position, new PixelSize(980, 640)).Bottom <= WorkArea.Bottom);
    }

    [Fact]
    public void IsPointerInsideWindow_uses_exclusive_right_and_bottom_edges()
    {
        var window = new PixelRect(100, 100, 300, 200);

        Assert.True(TileCornerTriggerPolicy.IsPointerInsideWindow(new PixelPoint(100, 100), window));
        Assert.False(TileCornerTriggerPolicy.IsPointerInsideWindow(new PixelPoint(400, 300), window));
    }
}
