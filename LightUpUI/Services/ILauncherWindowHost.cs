namespace LightUpUI.Services;

public interface ILauncherWindowHost
{
    bool IsVisible { get; }
    void Toggle();
    void Show();
    void Hide();
}
