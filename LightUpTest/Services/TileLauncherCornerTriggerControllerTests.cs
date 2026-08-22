using Avalonia;
using LightUpUI.Models;
using LightUpUI.Presentation;
using LightUpUI.Services;

namespace LightUpTest.Services;

public sealed class TileLauncherCornerTriggerControllerTests
{
    private static readonly PixelRect WorkArea = new(0, 0, 1920, 1080);

    [Fact]
    public void Poll_shows_once_after_the_cursor_dwelled_in_a_corner()
    {
        var cursor = new FakeCursor(new PixelPoint(2, 2));
        var host = new FakeCornerHost();
        using var controller = CreateController(cursor, host);
        var start = DateTimeOffset.UtcNow;

        controller.Poll(start);
        controller.Poll(start.AddMilliseconds(699));
        Assert.Equal(0, host.ShowCount);

        controller.Poll(start.AddMilliseconds(700));
        controller.Poll(start.AddMilliseconds(900));

        Assert.Equal(1, host.ShowCount);
        Assert.Equal(ScreenCorner.TopLeft, host.LastCorner);
    }

    [Fact]
    public void Poll_hides_an_auto_opened_window_after_pointer_leave()
    {
        var cursor = new FakeCursor(new PixelPoint(2, 2));
        var host = new FakeCornerHost { WindowBounds = new PixelRect(0, 0, 800, 500) };
        using var controller = CreateController(cursor, host);
        var start = DateTimeOffset.UtcNow;

        controller.Poll(start);
        controller.Poll(start.AddMilliseconds(700));
        cursor.Position = new PixelPoint(1000, 600);
        controller.Poll(start.AddMilliseconds(800));

        Assert.Equal(1, host.HideCount);
    }

    [Fact]
    public void Poll_keeps_the_window_when_pointer_is_inside_it()
    {
        var cursor = new FakeCursor(new PixelPoint(2, 2));
        var host = new FakeCornerHost { WindowBounds = new PixelRect(0, 0, 800, 500) };
        using var controller = CreateController(cursor, host);
        var start = DateTimeOffset.UtcNow;

        controller.Poll(start);
        controller.Poll(start.AddMilliseconds(700));
        cursor.Position = new PixelPoint(400, 250);
        controller.Poll(start.AddMilliseconds(800));

        Assert.Equal(0, host.HideCount);
    }

    [Fact]
    public void Poll_does_not_hide_when_pointer_leave_close_is_disabled()
    {
        var cursor = new FakeCursor(new PixelPoint(2, 2));
        var host = new FakeCornerHost { WindowBounds = new PixelRect(0, 0, 800, 500) };
        var settings = new SearchLauncherSettings
        {
            EnableTileCornerTrigger = true,
            CloseTileLauncherOnPointerLeave = false
        };
        using var controller = new TileLauncherCornerTriggerController(
            cursor,
            () => [new TileLauncherScreenArea(WorkArea, WorkArea)],
            host,
            settings);
        var start = DateTimeOffset.UtcNow;

        controller.Poll(start);
        controller.Poll(start.AddMilliseconds(700));
        cursor.Position = new PixelPoint(1000, 600);
        controller.Poll(start.AddMilliseconds(800));

        Assert.Equal(0, host.HideCount);
    }

    [Fact]
    public void Poll_does_not_close_immediately_when_bottom_corner_trigger_is_on_the_taskbar()
    {
        var cursor = new FakeCursor(new PixelPoint(2, 1078));
        var host = new FakeCornerHost { WindowBounds = new PixelRect(0, 440, 980, 640) };
        using var controller = new TileLauncherCornerTriggerController(
            cursor,
            () => [new TileLauncherScreenArea(
                new PixelRect(0, 0, 1920, 1080),
                new PixelRect(0, 0, 1920, 1040))],
            host,
            new SearchLauncherSettings
            {
                EnableTileCornerTrigger = true,
                CloseTileLauncherOnPointerLeave = true
            });
        var start = DateTimeOffset.UtcNow;

        controller.Poll(start);
        controller.Poll(start.AddMilliseconds(700));

        Assert.Equal(1, host.ShowCount);
        Assert.Equal(0, host.HideCount);

        cursor.Position = new PixelPoint(400, 700);
        controller.Poll(start.AddMilliseconds(800));
        cursor.Position = new PixelPoint(1200, 700);
        controller.Poll(start.AddMilliseconds(900));
        Assert.Equal(1, host.HideCount);
    }

    private static TileLauncherCornerTriggerController CreateController(FakeCursor cursor, FakeCornerHost host)
        => new(cursor, () => [new TileLauncherScreenArea(WorkArea, WorkArea)], host, new SearchLauncherSettings
        {
            EnableTileCornerTrigger = true,
            TileCornerTriggerDelayMilliseconds = 700
        });

    private sealed class FakeCursor(PixelPoint position) : ICursorPositionService
    {
        public PixelPoint Position { get; set; } = position;
        public PixelPoint GetPosition() => Position;
    }

    private sealed class FakeCornerHost : ITileLauncherCornerHost
    {
        public bool IsVisible { get; private set; }
        public bool IsCornerActivated { get; private set; }
        public PixelRect? WindowBounds { get; set; }
        public int ShowCount { get; private set; }
        public int HideCount { get; private set; }
        public ScreenCorner? LastCorner { get; private set; }

        public void ShowFromCorner(ScreenCorner corner, PixelRect workingArea)
        {
            IsVisible = true;
            IsCornerActivated = true;
            LastCorner = corner;
            ShowCount++;
        }

        public void Hide()
        {
            IsVisible = false;
            IsCornerActivated = false;
            HideCount++;
        }
    }
}
