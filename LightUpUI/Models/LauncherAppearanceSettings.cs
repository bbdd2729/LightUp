namespace LightUpUI.Models;

public sealed class LauncherAppearanceSettings
{
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
