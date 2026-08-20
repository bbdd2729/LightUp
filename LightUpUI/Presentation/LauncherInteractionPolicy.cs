namespace LightUpUI.Presentation;

public static class LauncherInteractionPolicy
{
    public static bool ShouldSelectOnClick(int clickCount) => clickCount >= 1;

    public static bool ShouldLaunchOnClick(int clickCount) => clickCount >= 2;
}
