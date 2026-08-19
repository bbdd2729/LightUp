namespace LightUpUI.Services;

public interface ITileLauncherWindowHost
{
    bool IsVisible { get; }
    void Toggle();
    void Show();
    void Hide();
}
