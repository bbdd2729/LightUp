namespace LightUpUI.Models;

public static class LauncherItemActionPolicy
{
    public static bool CanRevealLocation(LauncherItem item)
        => item.Kind != LauncherItemKind.Action
            && !string.IsNullOrWhiteSpace(item.LaunchPath);

    public static bool CanCopyLaunchPath(LauncherItem item)
        => !string.IsNullOrWhiteSpace(item.LaunchPath);

    public static bool CanRunAsAdministrator(LauncherItem item)
        => (item.Kind is LauncherItemKind.Application
            or LauncherItemKind.Shortcut
            or LauncherItemKind.PathExecutable
            or LauncherItemKind.File)
            && IsElevatablePath(item.LaunchPath);

    public static bool ShouldKeepSearchOpenAfterSuccess(LauncherItem item)
        => item.Id.Equals("action:copy-calculation", System.StringComparison.OrdinalIgnoreCase)
            || IsSearchQueryAction(item);

    public static bool IsSearchQueryAction(LauncherItem item)
        => item.Id.StartsWith("action:search-query:", System.StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(item.Arguments);

    private static bool IsElevatablePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var extension = System.IO.Path.GetExtension(path);
        return extension.Equals(".exe", System.StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".com", System.StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bat", System.StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cmd", System.StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".msi", System.StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".lnk", System.StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".appref-ms", System.StringComparison.OrdinalIgnoreCase);
    }
}
