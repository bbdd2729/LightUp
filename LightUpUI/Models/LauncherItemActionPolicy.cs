namespace LightUpUI.Models;

public static class LauncherItemActionPolicy
{
    public static bool CanRevealLocation(LauncherItem item)
        => item.Kind != LauncherItemKind.Action
            && !string.IsNullOrWhiteSpace(item.LaunchPath);

    public static bool CanCopyLaunchPath(LauncherItem item)
        => !string.IsNullOrWhiteSpace(item.LaunchPath);

    public static bool ShouldKeepSearchOpenAfterSuccess(LauncherItem item)
        => item.Id.Equals("action:copy-calculation", System.StringComparison.OrdinalIgnoreCase);
}
