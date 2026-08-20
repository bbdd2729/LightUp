namespace LightUpUI.Services;

public static class WindowChromePolicy
{
    public static bool ToggleTopmost(bool currentTopmost) => !currentTopmost;

    public static bool CanStartMoveDrag(bool isInteractiveChild) => !isInteractiveChild;
}
