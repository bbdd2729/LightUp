namespace LightUpUI.Models;

public sealed class LauncherWindowState
{
    public bool HasPosition { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsTopmost { get; set; }
}
