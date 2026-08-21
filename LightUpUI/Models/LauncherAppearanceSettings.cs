namespace LightUpUI.Models;

using LightUpUI.Models.Tiles;

public sealed class LauncherAppearanceSettings
{
    public TileDensity TileDensity { get; set; } = TileDensity.Compact;

    public LauncherWindowState SearchWindow { get; set; } = new()
    {
        Width = 680,
        Height = 460
    };

    public LauncherWindowState TileLauncherWindow { get; set; } = new()
    {
        Width = 980,
        Height = 640
    };
}
