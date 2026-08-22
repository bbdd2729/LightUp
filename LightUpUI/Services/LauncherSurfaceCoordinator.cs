using Avalonia;
using LightUpUI.Presentation;

namespace LightUpUI.Services;

/// <summary>
/// Keeps the two launcher surfaces mutually exclusive regardless of how they are opened.
/// </summary>
public sealed class LauncherSurfaceCoordinator :
    ILauncherWindowHost,
    ITileLauncherWindowHost,
    ITileLauncherCornerHost
{
    private readonly ILauncherWindowHost _searchHost;
    private readonly ITileLauncherWindowHost _tileHost;
    private readonly ITileLauncherCornerHost _tileCornerHost;

    public LauncherSurfaceCoordinator(
        ILauncherWindowHost searchHost,
        ITileLauncherWindowHost tileHost,
        ITileLauncherCornerHost tileCornerHost)
    {
        _searchHost = searchHost;
        _tileHost = tileHost;
        _tileCornerHost = tileCornerHost;
    }

    bool ILauncherWindowHost.IsVisible => _searchHost.IsVisible;
    bool ITileLauncherWindowHost.IsVisible => _tileHost.IsVisible;
    bool ITileLauncherCornerHost.IsVisible => _tileCornerHost.IsVisible;

    bool ITileLauncherCornerHost.IsCornerActivated => _tileCornerHost.IsCornerActivated;
    PixelRect? ITileLauncherCornerHost.WindowBounds => _tileCornerHost.WindowBounds;

    public void ToggleSearch()
    {
        ((ILauncherWindowHost)this).Toggle();
    }

    public void ToggleTiles()
    {
        ((ITileLauncherWindowHost)this).Toggle();
    }

    public void ShowSearch()
    {
        ((ILauncherWindowHost)this).Show();
    }

    public void ShowTiles()
    {
        ((ITileLauncherWindowHost)this).Show();
    }

    void ILauncherWindowHost.Toggle()
    {
        if (_searchHost.IsVisible)
            _searchHost.Hide();
        else
            ShowSearchCore();
    }

    void ITileLauncherWindowHost.Toggle()
    {
        if (_tileHost.IsVisible)
            _tileHost.Hide();
        else
            ShowTilesCore();
    }

    void ILauncherWindowHost.Show() => ShowSearchCore();
    void ITileLauncherWindowHost.Show() => ShowTilesCore();
    void ILauncherWindowHost.Hide() => _searchHost.Hide();
    void ITileLauncherWindowHost.Hide() => _tileHost.Hide();
    void ITileLauncherCornerHost.Hide() => _tileCornerHost.Hide();

    void ITileLauncherCornerHost.ShowFromCorner(ScreenCorner corner, PixelRect workingArea)
    {
        _searchHost.Hide();
        _tileCornerHost.ShowFromCorner(corner, workingArea);
    }

    private void ShowSearchCore()
    {
        _tileHost.Hide();
        _searchHost.Show();
    }

    private void ShowTilesCore()
    {
        _searchHost.Hide();
        _tileHost.Show();
    }
}
