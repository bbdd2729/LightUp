using Avalonia;
using LightUpUI.Presentation;

namespace LightUpUI.Services;

public interface ITileLauncherCornerHost
{
    bool IsVisible { get; }
    bool IsCornerActivated { get; }
    PixelRect? WindowBounds { get; }
    void ShowFromCorner(ScreenCorner corner, PixelRect workingArea);
    void Hide();
}
