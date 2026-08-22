using Avalonia;
using LightUpUI.Presentation;
using LightUpUI.Services;

namespace LightUpTest.Windowing;

public sealed class LauncherSurfaceCoordinatorTests
{
    [Fact]
    public void Showing_search_hides_tiles_and_toggling_search_hides_it()
    {
        var (coordinator, search, tiles) = CreateCoordinator();

        coordinator.ShowSearch();

        Assert.True(search.IsVisible);
        Assert.False(tiles.IsVisible);

        coordinator.ToggleSearch();

        Assert.False(search.IsVisible);
    }

    [Fact]
    public void Showing_tiles_hides_search_and_toggling_tiles_hides_it()
    {
        var (coordinator, search, tiles) = CreateCoordinator();
        search.IsVisible = true;

        coordinator.ShowTiles();

        Assert.False(search.IsVisible);
        Assert.True(tiles.IsVisible);

        coordinator.ToggleTiles();

        Assert.False(tiles.IsVisible);
    }

    [Fact]
    public void Showing_tiles_from_corner_hides_search_before_positioning_tiles()
    {
        var (coordinator, search, tiles) = CreateCoordinator();
        search.IsVisible = true;

        ((ITileLauncherCornerHost)coordinator).ShowFromCorner(
            ScreenCorner.BottomRight,
            new PixelRect(0, 0, 1920, 1080));

        Assert.False(search.IsVisible);
        Assert.True(tiles.IsCornerActivated);
        Assert.Equal(ScreenCorner.BottomRight, tiles.LastCorner);
    }

    private static (LauncherSurfaceCoordinator Coordinator, FakeSearchHost Search, FakeTileHost Tiles) CreateCoordinator()
    {
        var search = new FakeSearchHost();
        var tiles = new FakeTileHost();
        return (new LauncherSurfaceCoordinator(search, tiles, tiles), search, tiles);
    }

    private sealed class FakeSearchHost : ILauncherWindowHost
    {
        public bool IsVisible { get; set; }
        public void Toggle() => IsVisible = !IsVisible;
        public void Show() => IsVisible = true;
        public void Hide() => IsVisible = false;
    }

    private sealed class FakeTileHost : ITileLauncherWindowHost, ITileLauncherCornerHost
    {
        public bool IsVisible { get; private set; }
        public bool IsCornerActivated { get; private set; }
        public PixelRect? WindowBounds => IsVisible ? new PixelRect(0, 0, 800, 600) : null;
        public ScreenCorner? LastCorner { get; private set; }

        public void Toggle() => IsVisible = !IsVisible;
        public void Show()
        {
            IsVisible = true;
            IsCornerActivated = false;
        }

        public void Hide()
        {
            IsVisible = false;
            IsCornerActivated = false;
        }

        public void ShowFromCorner(ScreenCorner corner, PixelRect workingArea)
        {
            IsVisible = true;
            IsCornerActivated = true;
            LastCorner = corner;
        }
    }
}
