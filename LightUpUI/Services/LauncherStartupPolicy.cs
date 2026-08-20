namespace LightUpUI.Services;

public enum LauncherStartupSurface
{
    TileLauncher,
    SearchLauncher
}

public static class LauncherStartupPolicy
{
    public static LauncherStartupSurface MainSurface => LauncherStartupSurface.TileLauncher;

    public static bool ShouldShowMainSurfaceOnStartup => true;
}
